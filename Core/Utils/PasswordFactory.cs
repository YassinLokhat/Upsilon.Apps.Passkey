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
            || length == 0)
         {
            return string.Empty;
         }

         StringBuilder stringBuilder = new();
         Random random = new((int)DateTime.Now.Ticks);
         byte iteration = 0;

         do
         {
            iteration++;
            _ = stringBuilder.Clear();

            for (int i = 0; i < length; i++)
            {
               _ = stringBuilder.Append(alphabet[random.Next(alphabet.Length)]);
            }
         }
         while (iteration < length && checkIfLeaked && PasswordLeaked(stringBuilder.ToString()));

         return iteration == length ? string.Empty : stringBuilder.ToString();
      }

      public bool PasswordLeaked(string password)
      {
         string hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));

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

            return res.Contains(hash[5..]);
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
