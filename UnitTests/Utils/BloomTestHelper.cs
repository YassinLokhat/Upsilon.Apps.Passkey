using Upsilon.Apps.Passkey.Utils;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   internal static class BloomTestHelper
   {
      /// <summary>
      /// Well-known HIBP-leaked password used as a fixture (NTLM
      /// 0CB6948805F797BF2A82807973B89537).
      /// </summary>
      public const string LeakedPassword = "test";

      public static byte[] Ntlm(string password)
         => NtlmHash.Hash(password);

      public static string TempPkbfPath()
         => Path.Combine(Path.GetTempPath(), $"pkbf-{Guid.NewGuid():N}.pkbf");

      public static void WriteBloomContaining(string path, params string[] passwords)
      {
         const ulong capacity = 1_000;
         (ulong bits, int k) = BloomSizing.For(capacity, 0.01);
         using HibpBloomFile writable = HibpBloomFile.Create(path, capacity, bits, k);
         foreach (string password in passwords)
         {
            writable.Add(Ntlm(password));
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
