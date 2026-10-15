namespace Upsilon.Apps.Passkey.GUI.Presentation.Models
{
   /// <summary>
   /// The username names the vault file and is the first encryption layer, so
   /// login, creation, rename and confirmation must normalize it identically.
   /// </summary>
   internal static class CredentialRules
   {
      public static string NormalizeUsername(string? username) => (username ?? string.Empty).Trim();
   }
}
