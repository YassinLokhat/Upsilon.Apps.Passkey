using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   public class PasswordFactory : IPasswordFactory
   {
      // A single, shared HttpClient avoids the socket exhaustion caused by
      // creating (and disposing) one client per leak check. The timeout keeps a
      // slow or unreachable service from blocking password generation forever.
      private static readonly HttpClient _httpClient = new()
      {
         Timeout = TimeSpan.FromSeconds(10),
      };

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

         try
         {
            using HttpRequestMessage request = new(HttpMethod.Get, _rangeUri(hash));
            using HttpResponseMessage response = _httpClient.Send(request);

            if (!_succeeded(response))
            {
               return false;
            }

            using StreamReader reader = new(response.Content.ReadAsStream());

            return _isListed(reader.ReadToEnd(), hash);
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

         try
         {
            using HttpRequestMessage request = new(HttpMethod.Get, _rangeUri(hash));
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!_succeeded(response))
            {
               return false;
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return _isListed(body, hash);
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

      // The retry budget is a fixed number of attempts to find a non-leaked
      // password; it is intentionally independent of the requested length.
      private const int MAX_ATTEMPTS = 100;

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
      private static string _rangeUri(string hash) => $"https://api.pwnedpasswords.com/range/{hash[..5]}";

      private static bool _succeeded(HttpResponseMessage response)
      {
         if (response.IsSuccessStatusCode)
         {
            return true;
         }

         System.Diagnostics.Trace.TraceWarning($"Password leak check returned HTTP {(int)response.StatusCode}.");

         return false;
      }

      private static bool _isListed(string responseBody, string hash)
         => responseBody.Contains(hash[5..], StringComparison.Ordinal);

      private static bool _failOpen(Exception exception)
      {
         // A leak check must never crash password generation or the warning
         // scan. When the service is unreachable we cannot confirm a leak, so
         // we report "not leaked" and trace the failure for diagnostics.
         System.Diagnostics.Trace.TraceWarning($"Password leak check failed: {exception}");

         return false;
      }
   }
}
