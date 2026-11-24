using Upsilon.Apps.Passkey.GUI.Presentation.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>User-setting driven delays for secrets shown or copied from vault editors.</summary>
   internal static class ClipboardPolicy
   {
      /// <summary>Clipboard auto-clear delay in seconds from the user settings; 0 disables it.</summary>
      public static int AutoClearAfter
         => AppServices.Session.User?.Settings.CleaningClipboardTimeout ?? 0;

      /// <summary>QR display delay in milliseconds from the user settings; 0 keeps it until closed.</summary>
      public static int ShowSecretDelay
         => AppServices.Session.User?.Settings.ShowPasswordDelay ?? 0;

      /// <summary>Shows <paramref name="secret"/> as a QR code through the host dialogs.</summary>
      public static async Task ShowQrCodeAsync(string? secret)
      {
         if (!string.IsNullOrEmpty(secret) && AppServices.Dialogs is { } dialogs)
         {
            _ = await dialogs.ShowAsync(new ShowQrCodeRequest(secret, ShowSecretDelay)).ConfigureAwait(true);
         }
      }
   }
}
