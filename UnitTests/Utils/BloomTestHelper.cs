using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   internal static class BloomTestHelper
   {
      /// <summary>
      /// Well-known HIBP-leaked password used as a fixture (SHA-1
      /// A94A8FE5CCB19BA61C4C0873D391E987982FBBD3).
      /// </summary>
      public const string LeakedPassword = "test";

      public static byte[] Sha1(string password)
         => SHA1.HashData(Encoding.UTF8.GetBytes(password));

      public static string TempPkbfPath()
         => Path.Combine(Path.GetTempPath(), $"pkbf-{Guid.NewGuid():N}.pkbf");

      public static void WriteBloomContaining(string path, params string[] passwords)
      {
         const ulong capacity = 1_000;
         (ulong bits, int k) = BloomSizing.For(capacity, 0.01);
         using HibpBloomFile writable = HibpBloomFile.Create(path, capacity, bits, k);
         foreach (string password in passwords)
         {
            writable.Add(Sha1(password));
         }

         writable.CommitHeader();
      }

      public static void DeleteQuietly(string path)
      {
         if (File.Exists(path))
         {
            File.Delete(path);
         }
      }
   }
}
