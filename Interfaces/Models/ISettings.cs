using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Represent the user settings.
   /// </summary>
   public interface ISettings
   {
      /// <summary>
      /// The number of minutes of inactivity before auto-logout.
      /// </summary>
      int LogoutTimeout { get; set; }

      /// <summary>
      /// The number of second to keep existing passwords in the clipboard.
      /// </summary>
      int CleaningClipboardTimeout { get; set; }

      /// <summary>
      /// The delay to keep password visible.
      /// </summary>
      int ShowPasswordDelay { get; set; }

      /// <summary>
      /// The number of old paswords to keep.
      /// </summary>
      int NumberOfOldPasswordToKeep { get; set; }

      /// <summary>
      /// The number of months activities to keep.
      /// </summary>
      int NumberOfMonthActivitiesToKeep { get; set; }

      /// <summary>
      /// The warnings types which will be notified if detected.
      /// </summary>
      WarningType WarningsToNotify { get; set; }
   }
}
