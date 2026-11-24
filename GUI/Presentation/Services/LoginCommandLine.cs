using System.Security;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>
   /// Desktop launch arguments <c>[vault.pku] [username]</c>, as passed by
   /// <see cref="Environment.GetCommandLineArgs"/> (index 0 is the executable).
   /// </summary>
   internal static class LoginCommandLine
   {
      /// <summary>
      /// The vault is kept only when it is an existing local <c>.pku</c> file:
      /// UNC and device paths are refused because probing them can trigger
      /// network authentication. The username is returned as typed.
      /// </summary>
      public static (string? VaultPath, string? Username) Parse(IReadOnlyList<string> args)
      {
         ArgumentNullException.ThrowIfNull(args);

         string? vaultPath = args.Count > 1 ? _validVaultPath(args[1]) : null;
         string? username = args.Count > 2 ? args[2] : null;
         return (vaultPath, username);
      }

      private static string? _validVaultPath(string argument)
      {
         try
         {
            string path = Path.GetFullPath(argument);
            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
               AppServices.Log.Warn("Ignored command-line database path: network or device paths are not accepted.");
               return null;
            }

            if (!string.Equals(Path.GetExtension(path), ".pku", StringComparison.OrdinalIgnoreCase))
            {
               AppServices.Log.Warn("Ignored command-line database path: not a .pku file.");
               return null;
            }

            return File.Exists(path) ? path : null;
         }
         catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException or SecurityException)
         {
            AppServices.Log.Warn($"Ignored invalid database path from command line: {ErrorText.ForLog(ex)}");
            return null;
         }
      }
   }
}
