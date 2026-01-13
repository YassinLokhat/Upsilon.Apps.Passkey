using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   public class PasswordFactory : IPasswordFactory
   {
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

         using HttpClient httpClient = new();
         HttpRequestMessage request = new(HttpMethod.Get, $"https://api.pwnedpasswords.com/range/{hash[..5]}");
         HttpResponseMessage response = httpClient.Send(request);
         using StreamReader reader = new(response.Content.ReadAsStream());

         string res = reader.ReadToEnd();

         return res.Contains(hash[5..]);
      }
   }
}
