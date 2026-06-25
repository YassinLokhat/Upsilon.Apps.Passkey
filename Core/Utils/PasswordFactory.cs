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
         if (string.IsNullOrWhiteSpace(alphabet)
            || length <= 0)
         {
            return string.Empty;
         }

         // The retry budget is a fixed number of attempts to find a non-leaked
         // password; it is intentionally independent of the requested length.
         const int maxAttempts = 100;

         StringBuilder stringBuilder = new(length);

         for (int attempt = 0; attempt < maxAttempts; attempt++)
         {
            _ = stringBuilder.Clear();

            for (int i = 0; i < length; i++)
            {
               // RandomNumberGenerator.GetInt32 is a cryptographically secure,
               // unbiased source: unlike System.Random it cannot be predicted
               // from the current time, which is essential when minting secrets.
               _ = stringBuilder.Append(alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]);
            }

            string candidate = stringBuilder.ToString();

            if (!checkIfLeaked || !PasswordLeaked(candidate))
            {
               return candidate;
            }
         }

         // Every attempt produced a leaked password: give up rather than
         // returning a password that is known to be compromised.
         return string.Empty;
      }

      public bool PasswordLeaked(string password)
      {
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms : pwnedpasswords.com's API needs the use of SHA1 algorithm
         string hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms

         try
         {
            using HttpRequestMessage request = new(HttpMethod.Get, $"https://api.pwnedpasswords.com/range/{hash[..5]}");
            using HttpResponseMessage response = _httpClient.Send(request);

            if (!response.IsSuccessStatusCode)
            {
               System.Diagnostics.Trace.TraceWarning($"Password leak check returned HTTP {(int)response.StatusCode}.");
               return false;
            }

            using StreamReader reader = new(response.Content.ReadAsStream());
            string res = reader.ReadToEnd();

            return res.Contains(hash[5..], StringComparison.InvariantCulture);
         }
         catch (Exception ex)
         {
            // A leak check must never crash password generation or the warning
            // scan. When the service is unreachable we cannot confirm a leak, so
            // we report "not leaked" and trace the failure for diagnostics.
            System.Diagnostics.Trace.TraceWarning($"Password leak check failed: {ex}");
            return false;
         }
      }
   }
}
