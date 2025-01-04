namespace Upsilon.Apps.PassKey.Core.Utils
{
   internal static class SecurityCenter
   {
      public static string GetHash(this string source)
      {
         throw new NotImplementedException();
      }

      public static int HashLength => GetHash(string.Empty).Length;

      public static string Decrypt(string source, string[] passwords)
      {
         throw new NotImplementedException();
      }

      public static string Encrypt(string source, string[] passwords)
      {
         throw new NotImplementedException();
      }
   }
}
