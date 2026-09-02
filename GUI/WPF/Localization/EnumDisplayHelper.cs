using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// Localizes enum values stored in activity <c>FieldValue</c> (Core persists
   /// <see cref="Enum.ToString()"/> names, not translated text).
   /// </summary>
   internal static class EnumDisplayHelper
   {
      private const string ACCOUNT_OPTION_PREFIX = "EnumValue_AccountOption_";
      private const string WARNING_TYPE_PREFIX = "EnumValue_WarningType_";
      private const string IMPORT_EXPORT_ERROR_PREFIX = "EnumValue_ImportExportError_";

      public static string FormatFieldValue(string? fieldName, string? fieldValue)
      {
         return string.IsNullOrWhiteSpace(fieldValue)
            ? fieldValue ?? string.Empty
            : fieldName switch
            {
               nameof(AccountOption) or "Options" => _formatAccountOption(fieldValue),
               nameof(WarningType) or "WarningsToNotify" => _formatWarningType(fieldValue),
               "Theme" => _formatTheme(fieldValue),
               "Language" => _formatLanguage(fieldValue),
               nameof(ImportExportError) or "errorLog" => _formatImportExportError(fieldValue),
               _ => fieldValue,
            };
      }

      private static string _formatAccountOption(string stored)
      {
         return stored is "None" or "0" ? Strings.EnumValue_None : _formatFlags(stored, _accountOptionLabel);
      }

      private static string _formatWarningType(string stored)
      {
         return stored is "None" or "0" ? Strings.EnumValue_None : _formatFlags(stored, _warningTypeLabel);
      }

      private static string _formatTheme(string stored)
         => stored switch
         {
            ThemeService.SystemCode => Strings.EnumValue_Theme_System,
            ThemeService.LightCode => Strings.EnumValue_Theme_Light,
            ThemeService.DarkCode => Strings.EnumValue_Theme_Dark,
            _ => _isFollowApp(stored) ? Strings.EnumValue_FollowApp : stored,
         };

      private static string _formatLanguage(string stored)
         => stored is LocalizationService.SystemCode
            ? Strings.EnumValue_Theme_System
            : _isFollowApp(stored) ? Strings.EnumValue_FollowApp : stored;

      // Core now persists ISettings.FollowAppCode; older logs used "(app)".
      private static bool _isFollowApp(string stored)
         => stored is ISettings.FollowAppCode or "(app)";

      private static string _formatImportExportError(string stored)
         => stored is nameof(ImportExportError.None) or "0"
            ? Strings.EnumValue_ImportExportError_None
            : Strings.Get($"{IMPORT_EXPORT_ERROR_PREFIX}{stored}");

      private static string _formatFlags(string stored, Func<string, string> labelForMember)
      {
         string[] parts = stored.Contains(',', StringComparison.Ordinal)
            ? stored.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [stored.Trim()];

         return string.Join(", ", parts.Select(labelForMember));
      }

      private static string _accountOptionLabel(string memberName)
         => memberName switch
         {
            nameof(AccountOption.None) => Strings.EnumValue_None,
            nameof(AccountOption.WarnIfPasswordLeaked) => Strings.Label_WarnPasswordLeak,
            nameof(AccountOption.WarnIfDuplicatedPassword) => Strings.Label_WarnDuplicatedPassword,
            _ => Strings.Get($"{ACCOUNT_OPTION_PREFIX}{memberName}"),
         };

      private static string _warningTypeLabel(string memberName)
         => memberName switch
         {
            nameof(WarningType.ActivityReviewWarning) => Strings.Label_NotifyActivityReview,
            nameof(WarningType.PasswordUpdateReminderWarning) => Strings.Label_NotifyPasswordUpdateReminder,
            nameof(WarningType.DuplicatedPasswordsWarning) => Strings.Label_NotifyDuplicatedPasswords,
            nameof(WarningType.PasswordLeakedWarning) => Strings.Label_NotifyPasswordLeaked,
            nameof(WarningType.SecuritySettingsWarning) => Strings.Label_NotifySecuritySettings,
            _ => Strings.Get($"{WARNING_TYPE_PREFIX}{memberName}"),
         };
   }
}
