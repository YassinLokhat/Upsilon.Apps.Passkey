using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.Utils
{
   /// <summary>
   /// CSPRNG password generation and opt-in leak checks (HIBP, then XposedOrNot,
   /// then an optional offline Bloom filter).
   /// Network failures fail open: "not leaked", never cached.
   /// </summary>
   public class PasswordFactory : IPasswordFactory
   {
      private static readonly HttpClient _sharedHttpClient = new()
      {
         Timeout = TimeSpan.FromSeconds(3),
      };

      private const int MAX_ATTEMPTS = 5;
      private const int MAX_CACHED_RANGES = 512;
      private const int MAX_CACHED_XON_PREFIXES = 512;
      private const int HIBP_PREFIX_LENGTH = 5;
      private const int XON_PREFIX_LENGTH = 10;

      private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _send;
      private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;
      private ILocalLeakFilter? _localFilter;

      private readonly ConcurrentDictionary<string, HashSet<string>> _hibpRangeCache
         = new(StringComparer.OrdinalIgnoreCase);

      private readonly ConcurrentDictionary<string, bool> _xonPrefixCache
         = new(StringComparer.OrdinalIgnoreCase);

      public PasswordFactory()
         : this(
            static (request, cancellationToken) => _sharedHttpClient.Send(request, cancellationToken),
            static (request, cancellationToken) => _sharedHttpClient.SendAsync(request, cancellationToken))
      { }

      public PasswordFactory(LeakFilterConfig config)
         : this(
            static (request, cancellationToken) => _sharedHttpClient.Send(request, cancellationToken),
            static (request, cancellationToken) => _sharedHttpClient.SendAsync(request, cancellationToken))
      {
         ReloadLocalFilter(config);
      }

      /// <summary>
      /// Test seam: drives leak checks through custom send delegates (no real network).
      /// Does not auto-load the machine-level filter.
      /// </summary>
      internal PasswordFactory(
         Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> send,
         Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync,
         ILocalLeakFilter? localFilter = null)
      {
         _send = send ?? throw new ArgumentNullException(nameof(send));
         _sendAsync = sendAsync ?? throw new ArgumentNullException(nameof(sendAsync));
         _localFilter = localFilter;
      }

      /// <summary>
      /// Whether an offline Bloom filter is currently attached for post-network fallback.
      /// </summary>
      public bool HasLocalFilter => _localFilter is not null;

      /// <summary>
      /// Replaces the offline filter. Pass <see langword="null"/> to detach without
      /// deleting the on-disk <c>.pkbf</c>.
      /// </summary>
      public void AttachLocalFilter(ILocalLeakFilter? filter)
      {
         if (ReferenceEquals(_localFilter, filter))
         {
            return;
         }

         _localFilter?.Dispose();
         _localFilter = filter;
      }

      /// <summary>
      /// Loads the filter configured in <paramref name="config"/> when enabled and present.
      /// </summary>
      public void ReloadLocalFilter(LeakFilterConfig config)
      {
         if (config is null)
         {
            return;
         }

         // Keep the current filter if re-open fails while the .pkbf still exists
         // (e.g. file still mapped by this instance).
         ILocalLeakFilter? replaced = _localFilter;
         _localFilter = config.TryOpenConfiguredFilter();

         if (_localFilter is null && replaced is not null && config.Enabled && File.Exists(config.FilterPath))
         {
            _localFilter = replaced;
            return;
         }

         replaced?.Dispose();
      }

      public string Alphabetic => "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
      public string Numeric => "0123456789";
      public string SpecialChars => "~!@#$%^&*()_-+={[}]\\|'\";:,<.>/?";

      public string GeneratePassword(int length, string alphabet, bool checkIfLeaked = true)
         => _candidates(length, alphabet).FirstOrDefault(x => !checkIfLeaked || !PasswordLeaked(x)) ?? string.Empty;

      public async Task<string> GeneratePasswordAsync(int length, string alphabet, bool checkIfLeaked = true, CancellationToken cancellationToken = default)
      {
         IEnumerable<string> candidates = _candidates(length, alphabet);

         if (!checkIfLeaked)
         {
            return candidates.FirstOrDefault() ?? string.Empty;
         }

         using IEnumerator<string> enumerator = candidates.GetEnumerator();
         while (enumerator.MoveNext())
         {
            string candidate = enumerator.Current;
            if (!await PasswordLeakedAsync(candidate, cancellationToken).ConfigureAwait(false))
            {
               return candidate;
            }
         }

         // Give up rather than returning a password known to be in a breach corpus.
         return string.Empty;
      }

      public bool PasswordLeaked(string password)
      {
         try
         {
            bool? hibp = _tryHibp(password);
            if (hibp.HasValue)
            {
               return hibp.Value;
            }

            bool? xon = _tryXon(password);
            if (xon.HasValue)
            {
               return xon.Value;
            }

            bool? bloom = _tryLocalBloom(password);
            if (bloom.HasValue)
            {
               return bloom.Value;
            }
         }
         catch (OperationCanceledException ex)
         {
            return _failOpen(ex);
         }

         return _failOpen(null);
      }

      public async Task<bool> PasswordLeakedAsync(string password, CancellationToken cancellationToken = default)
      {
         try
         {
            bool? hibp = await _tryHibpAsync(password, cancellationToken).ConfigureAwait(false);
            if (hibp.HasValue)
            {
               return hibp.Value;
            }

            bool? xon = await _tryXonAsync(password, cancellationToken).ConfigureAwait(false);
            if (xon.HasValue)
            {
               return xon.Value;
            }

            bool? bloom = _tryLocalBloom(password);
            if (bloom.HasValue)
            {
               return bloom.Value;
            }
         }
         // Caller cancellation must propagate; client timeout is fail-open below.
         catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
         {
            throw;
         }

         return _failOpen(null);
      }

      /// <summary>
      /// Offline Bloom after HIBP/XON failed. Null when no filter is attached.
      /// </summary>
      private bool? _tryLocalBloom(string password)
      {
         ILocalLeakFilter? filter = _localFilter;
         if (filter is null)
         {
            return null;
         }

         bool hit = filter.MightContain(NtlmHash.Hash(password));
         System.Diagnostics.Trace.TraceWarning(
            hit
               ? "Password leak check: HIBP and XposedOrNot unreachable; offline Bloom reported a possible hit (treated as leaked)."
               : "Password leak check: HIBP and XposedOrNot unreachable; offline Bloom miss (not leaked).");
         return hit;
      }

      /// <summary>
      /// HIBP: definitive yes/no on HTTP success, otherwise null (try XON).
      /// </summary>
      private bool? _tryHibp(string password)
      {
         string hash = NtlmHash.HashHex(password);
         string prefix = hash[..HIBP_PREFIX_LENGTH];

         if (_hibpRangeCache.TryGetValue(prefix, out HashSet<string>? suffixes))
         {
            return suffixes.Contains(hash[HIBP_PREFIX_LENGTH..]);
         }

         try
         {
            using HttpRequestMessage request = new(HttpMethod.Get, _hibpRangeUri(prefix));
            using HttpResponseMessage response = _send(request, CancellationToken.None);

            if (!response.IsSuccessStatusCode)
            {
               System.Diagnostics.Trace.TraceWarning(
                  $"HIBP leak check returned HTTP {(int)response.StatusCode}; trying XposedOrNot.");
               return null;
            }

            using StreamReader reader = new(response.Content.ReadAsStream());
            HashSet<string> parsed = _parseAndCacheHibp(prefix, reader.ReadToEnd());
            return parsed.Contains(hash[HIBP_PREFIX_LENGTH..]);
         }
         catch (Exception ex)
            when (ex is OutOfMemoryException
            or IOException
            or HttpRequestException)
         {
            System.Diagnostics.Trace.TraceWarning($"HIBP leak check failed ({ex.GetType().Name}); trying XposedOrNot.");
            return null;
         }
      }

      private async Task<bool?> _tryHibpAsync(string password, CancellationToken cancellationToken)
      {
         string hash = NtlmHash.HashHex(password);
         string prefix = hash[..HIBP_PREFIX_LENGTH];

         if (_hibpRangeCache.TryGetValue(prefix, out HashSet<string>? suffixes))
         {
            return suffixes.Contains(hash[HIBP_PREFIX_LENGTH..]);
         }

         try
         {
            using HttpRequestMessage request = new(HttpMethod.Get, _hibpRangeUri(prefix));
            using HttpResponseMessage response = await _sendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
               System.Diagnostics.Trace.TraceWarning(
                  $"HIBP leak check returned HTTP {(int)response.StatusCode}; trying XposedOrNot.");
               return null;
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            HashSet<string> parsed = _parseAndCacheHibp(prefix, body);
            return parsed.Contains(hash[HIBP_PREFIX_LENGTH..]);
         }
         catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
         {
            throw;
         }
         catch (Exception ex)
            when (ex is HttpRequestException
            or IOException
            or OperationCanceledException)
         {
            System.Diagnostics.Trace.TraceWarning($"HIBP leak check failed ({ex.GetType().Name}); trying XposedOrNot.");
            return null;
         }
      }

      /// <summary>
      /// XposedOrNot: 200 = leaked, 404 = not found, otherwise null.
      /// </summary>
      private bool? _tryXon(string password)
      {
         string hash = Keccak512.HashHex(password);
         string prefix = hash[..XON_PREFIX_LENGTH];

         if (_xonPrefixCache.TryGetValue(prefix, out bool cached))
         {
            return cached;
         }

         try
         {
            using HttpRequestMessage request = new(HttpMethod.Get, _xonUri(prefix));
            using HttpResponseMessage response = _send(request, CancellationToken.None);
            return _interpretXonResponse(prefix, response);
         }
         catch (Exception ex)
            when (ex is OutOfMemoryException
            or IOException
            or OperationCanceledException
            or HttpRequestException)
         {
            System.Diagnostics.Trace.TraceWarning($"XposedOrNot leak check failed: {ex}");
            return null;
         }
      }

      private async Task<bool?> _tryXonAsync(string password, CancellationToken cancellationToken)
      {
         string hash = Keccak512.HashHex(password);
         string prefix = hash[..XON_PREFIX_LENGTH];

         if (_xonPrefixCache.TryGetValue(prefix, out bool cached))
         {
            return cached;
         }

         try
         {
            using HttpRequestMessage request = new(HttpMethod.Get, _xonUri(prefix));
            using HttpResponseMessage response = await _sendAsync(request, cancellationToken).ConfigureAwait(false);
            return _interpretXonResponse(prefix, response);
         }
         catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
         {
            throw;
         }
         catch (Exception ex)
            when (ex is HttpRequestException
            or IOException
            or OperationCanceledException)
         {
            System.Diagnostics.Trace.TraceWarning($"XposedOrNot leak check failed ({ex.GetType().Name}); trying local bloom.");
            return null;
         }
      }

      private bool? _interpretXonResponse(string prefix, HttpResponseMessage response)
      {
         if (response.StatusCode == HttpStatusCode.NotFound)
         {
            _cacheXon(prefix, leaked: false);
            return false;
         }

         if (!response.IsSuccessStatusCode)
         {
            System.Diagnostics.Trace.TraceWarning(
               $"XposedOrNot leak check returned HTTP {(int)response.StatusCode}.");
            return null;
         }

         // HTTP 200 = leaked (body not required for the boolean).
         _cacheXon(prefix, leaked: true);
         return true;
      }

      private static IEnumerable<string> _candidates(int length, string alphabet)
      {
         if (string.IsNullOrWhiteSpace(alphabet)
            || length <= 0)
         {
            yield break;
         }

         StringBuilder stringBuilder = new(length);

         for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
         {
            _ = stringBuilder.Clear();

            for (int i = 0; i < length; i++)
            {
               _ = stringBuilder.Append(alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]);
            }

            yield return stringBuilder.ToString();
         }
      }

      private static string _hibpRangeUri(string prefix)
         => $"https://api.pwnedpasswords.com/range/{prefix}?mode=ntlm";

      private static string _xonUri(string prefix)
         => $"https://passwords.xposedornot.com/api/v1/pass/anon/{prefix}";

      private HashSet<string> _parseAndCacheHibp(string prefix, string body)
      {
         HashSet<string> suffixes = _parseSuffixes(body);

         if (_hibpRangeCache.Count >= MAX_CACHED_RANGES)
         {
            _hibpRangeCache.Clear();
         }

         _ = _hibpRangeCache.TryAdd(prefix, suffixes);

         return suffixes;
      }

      private void _cacheXon(string prefix, bool leaked)
      {
         if (_xonPrefixCache.Count >= MAX_CACHED_XON_PREFIXES)
         {
            _xonPrefixCache.Clear();
         }

         _ = _xonPrefixCache.TryAdd(prefix, leaked);
      }

      private static HashSet<string> _parseSuffixes(string body)
      {
         HashSet<string> suffixes = new(StringComparer.OrdinalIgnoreCase);

         foreach (string line in body.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
         {
            int separator = line.IndexOf(':', StringComparison.Ordinal);
            string suffix = separator >= 0 ? line[..separator] : line;

            if (suffix.Length != 0)
            {
               _ = suffixes.Add(suffix);
            }
         }

         return suffixes;
      }

      private static bool _failOpen(Exception? exception)
      {
         if (exception is null)
         {
            System.Diagnostics.Trace.TraceWarning(
               "Password leak check failed open: HIBP and XposedOrNot were unreachable and no offline Bloom filter is attached.");
         }
         else
         {
            System.Diagnostics.Trace.TraceWarning($"Password leak check failed: {exception}");
         }

         return false;
      }

      /// <summary>
      /// Number of HIBP ranges currently held in the in-process cache.
      /// </summary>
      internal int CachedRangeCount => _hibpRangeCache.Count;

      /// <summary>
      /// Number of XposedOrNot prefix answers currently held in the in-process cache.
      /// </summary>
      internal int CachedXonPrefixCount => _xonPrefixCache.Count;
   }
}
