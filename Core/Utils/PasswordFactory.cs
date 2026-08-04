using System.Collections.Concurrent;
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

      // Cap how many times we ask HIBP while hunting for a non-leaked candidate.
      // A strong random password from a wide alphabet is vanishingly unlikely to
      // be in the corpus, so a handful of attempts is enough; 100 remote calls
      // would only punish the user when the service is slow or every candidate
      // happens to collide.
      private const int MAX_ATTEMPTS = 5;

      // Bound the in-process prefix cache so a long session cannot grow without
      // limit. Eviction drops the whole table: the next checks simply refill it.
      private const int MAX_CACHED_RANGES = 512;

      private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _send;
      private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

      // k-anonymity ranges are keyed by the first five hex chars of the SHA-1
      // hash. Caching the parsed suffix set means a second check for the same
      // prefix (same password, or another password that shares the prefix) is a
      // local lookup - no network round-trip.
      private readonly ConcurrentDictionary<string, HashSet<string>> _rangeCache
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
         string hash = _sha1Hex(password);
         string prefix = hash[..5];

         if (_rangeCache.TryGetValue(prefix, out HashSet<string>? suffixes))
         {
            return suffixes.Contains(hash[5..]);
         }

         try
         {
            using HttpRequestMessage request = new(HttpMethod.Get, _rangeUri(prefix));
            using HttpResponseMessage response = _send(request, CancellationToken.None);

            if (!_succeeded(response))
            {
               return false;
            }

            using StreamReader reader = new(response.Content.ReadAsStream());
            HashSet<string> parsed = _parseAndCache(prefix, reader.ReadToEnd());

            return parsed.Contains(hash[5..]);
         }
#pragma warning disable CA1031 // Last-resort barrier: a leak check must never crash password generation
         catch (Exception ex)
#pragma warning restore CA1031
         {
            return _failOpen(ex);
         }
      }

      public async Task<bool> PasswordLeakedAsync(string password, CancellationToken cancellationToken = default)
      {
         string hash = _sha1Hex(password);
         string prefix = hash[..5];

         if (_rangeCache.TryGetValue(prefix, out HashSet<string>? suffixes))
         {
            return suffixes.Contains(hash[5..]);
         }

         try
         {
            using HttpRequestMessage request = new(HttpMethod.Get, _rangeUri(prefix));
            using HttpResponseMessage response = await _sendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!_succeeded(response))
            {
               return false;
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            HashSet<string> parsed = _parseAndCache(prefix, body);

            return parsed.Contains(hash[5..]);
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
      private static string _rangeUri(string prefix) => $"https://api.pwnedpasswords.com/range/{prefix}";

      private HashSet<string> _parseAndCache(string prefix, string body)
      {
         HashSet<string> suffixes = _parseSuffixes(body);

         if (_rangeCache.Count >= MAX_CACHED_RANGES)
         {
            _rangeCache.Clear();
         }

         _ = _rangeCache.TryAdd(prefix, suffixes);

         return suffixes;
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

      private static bool _succeeded(HttpResponseMessage response)
      {
         if (response.IsSuccessStatusCode)
         {
            return true;
         }

         System.Diagnostics.Trace.TraceWarning($"Password leak check returned HTTP {(int)response.StatusCode}.");

         return false;
      }

      private static bool _failOpen(Exception exception)
      {
         // A leak check must never crash password generation or the warning
         // scan. When the service is unreachable we cannot confirm a leak, so
         // we report "not leaked" and trace the failure for diagnostics.
         System.Diagnostics.Trace.TraceWarning($"Password leak check failed: {exception}");

         return false;
      }

      /// <summary>
      /// Number of HIBP ranges currently held in the in-process cache.
      /// </summary>
      internal int CachedRangeCount => _rangeCache.Count;
   }
}
