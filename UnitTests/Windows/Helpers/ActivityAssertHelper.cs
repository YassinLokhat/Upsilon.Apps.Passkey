using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Helpers
{
   /// <summary>
   /// Localized activity-line helpers for GUI / localization tests.
   /// Vault tests assert structured <see cref="IActivity"/> fields in Multiplateform.
   /// </summary>
   public static class ActivityAssertHelper
   {
      public static string FormatActivityLine(bool needsReview, string message)
      {
         message = message.Trim();
         message = message[..1].ToUpperInvariant() + message[1..];
         return $"{(needsReview ? "Warning" : "Information")} : {message}";
      }

      public static string FormatImportStarted(string filePath)
         => FormatActivityLine(true, Strings.Format(nameof(Strings.Activity_ImportingDataStarted), filePath));

      public static string FormatImportSucceeded()
         => FormatActivityLine(true, Strings.Activity_ImportingDataSucceeded);

      public static string FormatImportFailed(ImportExportError error)
      {
         string reason = EnumDisplayHelper.FormatFieldValue(nameof(ImportExportError), error.ToString());
         return FormatActivityLine(true, Strings.Format(nameof(Strings.Activity_ImportingDataFailed), reason));
      }

      public static string FormatExportStarted(string filePath)
         => FormatActivityLine(true, Strings.Format(nameof(Strings.Activity_ExportingDataStarted), filePath));

      public static string FormatExportSucceeded()
         => FormatActivityLine(true, Strings.Activity_ExportingDataSucceeded);

      public static string FormatExportFailed(ImportExportError error)
      {
         string reason = EnumDisplayHelper.FormatFieldValue(nameof(ImportExportError), error.ToString());
         return FormatActivityLine(true, Strings.Format(nameof(Strings.Activity_ExportingDataFailed), reason));
      }

      public static string FormatDatabaseSaved(string username)
         => FormatActivityLine(false, Strings.Format(nameof(Strings.Activity_DatabaseSaved), username));

      public static string MessageOf(IActivity activity)
         => new ActivityViewModel(activity).Message;
   }
}
