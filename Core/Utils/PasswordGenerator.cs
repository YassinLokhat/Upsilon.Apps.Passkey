using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Upsilon.Apps.PassKey.Core.Utils
{
   /// <summary>
   /// Contains the password generation methods.
   /// </summary>
   public static class PasswordGenerator
   {
      public static string Alphabetic { get => "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; }
      public static string Numeric { get => "0123456789"; }
      public static string SpecialChars { get => "~!@#$%^&*()_-+={[}]\\|'\";:,<.>/?"; }

      public static string GeneratePassword(int length,
         bool includeUpperCaseAlphabeticChars = true,
         bool includeLowerCaseAlphabeticChars = true,
         bool includeNumericChars = true,
         bool includeSpecialChars = true,
         string excludedChars = "",
         bool checkIfLeaked = true)
      {
         string alphabet = "";

         if (includeUpperCaseAlphabeticChars)
         {
            alphabet += Alphabetic.ToUpper();
         }

         if (includeLowerCaseAlphabeticChars)
         {
            alphabet += Alphabetic.ToLower();
         }

         if (includeNumericChars)
         {
            alphabet += Numeric;
         }

         if (includeSpecialChars)
         {
            alphabet += SpecialChars;
         }

         alphabet = string.Join("", alphabet.ToCharArray().Except(excludedChars.ToCharArray()));

         return GeneratePassword(length, alphabet, checkIfLeaked);
      }

      public static string GeneratePassword(int length, string alphabet, bool checkIfLeaked = true)
      {
         if (string.IsNullOrWhiteSpace(alphabet)
            || length == 0)
         {
            return string.Empty;
         }

         StringBuilder stringBuilder = new();
         Random random = new((int)DateTime.Now.Ticks);

         do
         {
            stringBuilder.Clear();

            for (int i = 0; i < length; i++)
            {
               _ = stringBuilder.Append(alphabet[random.Next(alphabet.Length)]);
            }
         }
         while (checkIfLeaked && PasswordLeaked(stringBuilder.ToString()));

         return stringBuilder.ToString();
      }

      public static bool PasswordLeaked(string password)
      {
         string hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));

         ServicePointManager.Expect100Continue = true;
         ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
         ServicePointManager.DefaultConnectionLimit = 9999;

         using HttpClient httpClient = new();
         HttpRequestMessage request = new(HttpMethod.Get, $"https://api.pwnedpasswords.com/range/{hash[..5]}");
         HttpResponseMessage response = httpClient.Send(request);
         using StreamReader reader = new(response.Content.ReadAsStream());

         string res = reader.ReadToEnd();

         return res.Contains(hash[5..]);
      }
   }
}
