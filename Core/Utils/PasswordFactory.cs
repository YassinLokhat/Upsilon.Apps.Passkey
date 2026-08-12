using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   public class PasswordFactory : IPasswordFactory
   {
      // A single, shared HttpClient avoids the socket exhaustion caused by
      // creating (and disposing) one client per leak check. The short timeout
      // keeps a slow or unreachable service from blocking generation or the
      // warning scan for long.
      private static readonly HttpClient _sharedHttpClient = new()
      {
         Timeout = TimeSpan.FromSeconds(3),
      };

      // Cap how many times we ask leak providers while hunting for a non-leaked
      // candidate. A strong random password from a wide alphabet is vanishingly
      // unlikely to be in the corpus, so a handful of attempts is enough; 100
      // remote calls would only punish the user when the service is slow or
      // every candidate happens to collide.
      private const int MAX_ATTEMPTS = 5;

      // Bound the in-process caches so a long session cannot grow without
      // limit. Eviction drops the whole table: the next checks simply refill it.
      private const int MAX_CACHED_RANGES = 512;
      private const int MAX_CACHED_XON_PREFIXES = 512;

      private const int HibpPrefixLength = 5;
      private const int XonPrefixLength = 10;

      private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _send;
      private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

      // HIBP k-anonymity ranges are keyed by the first five hex chars of the
      // SHA-1 hash. Caching the parsed suffix set means a second check for the
      // same prefix is a local lookup - no network round-trip.
      private readonly ConcurrentDictionary<string, HashSet<string>> _hibpRangeCache
         = new(StringComparer.OrdinalIgnoreCase);

      // XON answers yes/no for a Keccak-512 hash prefix; cache the boolean so a
      // repeated failover (or a second account with the same password) skips the
      // network. Only definitive answers are stored - never transport failures.
      private readonly ConcurrentDictionary<string, bool> _xonPrefixCache
         = new(StringComparer.OrdinalIgnoreCase);

      public PasswordFactory()
         : this(
            static (request, cancellationToken) => _sharedHttpClient.Send(request, cancellationToken),
            static (request, cancellationToken) => _sharedHttpClient.SendAsync(request, cancellationToken))
      {
      }

      /// <summary>
      /// Test seam: drives leak checks through custom send delegates (no real network).
      /// </summary>
      internal PasswordFactory(
         Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> send,
         Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
      {
         _send = send ?? throw new ArgumentNullException(nameof(send));
         _sendAsync = sendAsync ?? throw new ArgumentNullException(nameof(sendAsync));
      }

      public string Alphabetic => "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
      public string Numeric => "0123456789";
      public string SpecialChars => "~!@#$%^&*()_-+={[}]\\|'\";:,<.>/?";

      public string GeneratePassword(int length, string alphabet, bool checkIfLeaked = true)
      {
         foreach (string candidate in _candidates(length, alphabet))
         {
            if (!checkIfLeaked || !PasswordLeaked(candidate))
            {
               return candidate;
            }
         }

         // Every attempt produced a leaked password: give up rather than
         // returning a password that is known to be compromised.
         return string.Empty;
      }

      public async Task<string> GeneratePasswordAsync(int length, string alphabet, bool checkIfLeaked = true, CancellationToken cancellationToken = default)
      {
         foreach (string candidate in _candidates(length, alphabet))
         {
            if (!checkIfLeaked || !await PasswordLeakedAsync(candidate, cancellationToken).ConfigureAwait(false))
            {
               return candidate;
            }
         }

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
         }
#pragma warning disable CA1031 // Last-resort barrier: a leak check must never crash password generation
         catch (Exception ex)
#pragma warning restore CA1031
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
         }
         // An explicit cancellation is the caller's decision and must surface as
         // such; the client's own timeout also lands here as an
         // OperationCanceledException, and is handled below as a failure to
         // reach the service.
         catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
         {
            throw;
         }
#pragma warning disable CA1031 // Last-resort barrier: a leak check must never crash password generation
         catch (Exception ex)
#pragma warning restore CA1031
         {
            return _failOpen(ex);
         }

         return _failOpen(null);
      }

      /// <summary>
      /// Queries HIBP. Returns a definitive yes/no on HTTP success, or
      /// <see langword="null"/> when the service is unreachable so the caller
      /// can fall through to XposedOrNot.
      /// </summary>
      private bool? _tryHibp(string password)
      {
         string hash = _sha1Hex(password);
         string prefix = hash[..HibpPrefixLength];

         if (_hibpRangeCache.TryGetValue(prefix, out HashSet<string>? suffixes))
         {
            return suffixes.Contains(hash[HibpPrefixLength..]);
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
            return parsed.Contains(hash[HibpPrefixLength..]);
         }
#pragma warning disable CA1031 // Provider-local barrier: fall through to XON instead of aborting the whole check
         catch (Exception ex)
#pragma warning restore CA1031
         {
            System.Diagnostics.Trace.TraceWarning($"HIBP leak check failed ({ex.GetType().Name}); trying XposedOrNot.");
            return null;
         }
      }

      private async Task<bool?> _tryHibpAsync(string password, CancellationToken cancellationToken)
      {
         string hash = _sha1Hex(password);
         string prefix = hash[..HibpPrefixLength];

         if (_hibpRangeCache.TryGetValue(prefix, out HashSet<string>? suffixes))
         {
            return suffixes.Contains(hash[HibpPrefixLength..]);
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
            return parsed.Contains(hash[HibpPrefixLength..]);
         }
         catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
         {
            throw;
         }
#pragma warning disable CA1031 // Provider-local barrier: fall through to XON instead of aborting the whole check
         catch (Exception ex)
#pragma warning restore CA1031
         {
            System.Diagnostics.Trace.TraceWarning($"HIBP leak check failed ({ex.GetType().Name}); trying XposedOrNot.");
            return null;
         }
      }

      /// <summary>
      /// Queries XposedOrNot's anonymous password API. Returns a definitive
      /// yes/no on HTTP 200 (leaked) or 404 (not found), or
      /// <see langword="null"/> when the service is unreachable.
      /// </summary>
      private bool? _tryXon(string password)
      {
         string hash = Keccak512.HashHex(password);
         string prefix = hash[..XonPrefixLength];

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
#pragma warning disable CA1031 // Provider-local barrier: outer PasswordLeaked still fails open
         catch (Exception ex)
#pragma warning restore CA1031
         {
            System.Diagnostics.Trace.TraceWarning($"XposedOrNot leak check failed: {ex}");
            return null;
         }
      }

      private async Task<bool?> _tryXonAsync(string password, CancellationToken cancellationToken)
      {
         string hash = Keccak512.HashHex(password);
         string prefix = hash[..XonPrefixLength];

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
#pragma warning disable CA1031 // Provider-local barrier: outer PasswordLeakedAsync still fails open
         catch (Exception ex)
#pragma warning restore CA1031
         {
            System.Diagnostics.Trace.TraceWarning($"XposedOrNot leak check failed: {ex}");
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

         // 200 with a SearchPassAnon payload means the prefix matched a known
         // exposed password. Any successful 200 is treated as leaked; the body
         // is not required for the boolean decision.
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
               // RandomNumberGenerator.GetInt32 is a cryptographically secure,
               // unbiased source: unlike System.Random it cannot be predicted
               // from the current time, which is essential when minting secrets.
               _ = stringBuilder.Append(alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]);
            }

            yield return stringBuilder.ToString();
         }
      }

      private static string _sha1Hex(string password)
      {
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms : pwnedpasswords.com's API needs the use of SHA1 algorithm
         return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
      }

      // k-anonymity: only the first five characters of the hash ever leave the
      // machine, so the service never learns which password is being checked.
      private static string _hibpRangeUri(string prefix)
         => $"https://api.pwnedpasswords.com/range/{prefix}";

      // XON k-anonymity: first 10 hex chars of Keccak-512; password and full
      // hash never leave the machine.
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
         // A leak check must never crash password generation or the warning
         // scan. When every provider is unreachable we cannot confirm a leak,
         // so we report "not leaked" and trace the failure for diagnostics.
         if (exception is null)
         {
            System.Diagnostics.Trace.TraceWarning(
               "Password leak check failed open: HIBP and XposedOrNot were both unreachable.");
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
