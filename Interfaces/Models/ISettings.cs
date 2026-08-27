using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface ISettings
   {
      /// <summary>Minutes of inactivity before auto-logout.</summary>
      int LogoutTimeout { get; set; }

      /// <summary>Seconds to keep a copied secret on the clipboard.</summary>
      int CleaningClipboardTimeout { get; set; }

      /// <summary>
      /// Milliseconds to keep a QR-code window open (<c>0</c> = until dismissed).
      /// Named historically for password reveal; the WPF client uses it for QR display.
      /// </summary>
      int ShowPasswordDelay { get; set; }

      /// <summary>Max dated entries kept in password history.</summary>
      int NumberOfOldPasswordToKeep { get; set; }

      /// <summary>Months of activity history to retain.</summary>
      int NumberOfMonthActivitiesToKeep { get; set; }

      /// <summary>Which warning kinds to surface to the user.</summary>
      WarningType WarningsToNotify { get; set; }

      /// <summary>
      /// UI language override (<c>en</c>, <c>fr</c>, …). Empty means follow the
      /// application language from <c>config.json</c>.
      /// </summary>
      string Language { get; set; }

      /// <summary>
      /// UI theme override (<c>System</c>, <c>Light</c>, <c>Dark</c>). Empty means
      /// follow the application theme from <c>config.json</c>.
      /// </summary>
      string Theme { get; set; }
   }
}
