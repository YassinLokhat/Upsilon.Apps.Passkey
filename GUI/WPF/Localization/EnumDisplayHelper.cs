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
#pragma warning disable CS0618 // Legacy activity logs may still store WarningType names.
               nameof(WarningType) or "AlertsToNotify" or "WarningsToNotify" => _formatAlertsToNotify(fieldValue),
#pragma warning restore CS0618
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

      private static string _formatAlertsToNotify(string stored)
      {
         if (stored is "None" or "[]" or "0")
         {
            return Strings.EnumValue_None;
         }

         // New format: JSON array or comma-separated kind ids.
         if (stored.StartsWith('[') && stored.EndsWith(']'))
         {
            string inner = stored[1..^1].Trim();
            if (string.IsNullOrEmpty(inner))
            {
               return Strings.EnumValue_None;
            }

            string[] kinds = [.. inner
               .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
               .Select(static p => p.Trim().Trim('"'))];
            return string.Join(", ", kinds.Select(_warningKindLabel));
         }

         if (stored.Contains(',', StringComparison.Ordinal)
            || AlertKinds.DefaultNotify.Contains(stored)
            || AlertKinds.AllCore.Contains(stored)
            || AlertKinds.AllHost.Contains(stored))
         {
            return _formatFlags(stored, _warningKindLabel);
         }

#pragma warning disable CS0618 // Legacy WarningType flag / name migration for activity logs.
         if (Enum.TryParse(stored, ignoreCase: true, out WarningType legacyFromName))
         {
            return string.Join(", ", AlertKinds.FromLegacyWarningType(legacyFromName).Select(_warningKindLabel));
         }

         if (int.TryParse(stored, out int legacyNumber))
         {
            return string.Join(", ", AlertKinds.FromLegacyWarningType((WarningType)legacyNumber).Select(_warningKindLabel));
         }
#pragma warning restore CS0618

         return _formatFlags(stored, _warningKindLabel);
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

      private static string _warningKindLabel(string kindOrLegacyMember)
#pragma warning disable CS0618 // Legacy activity-log WarningType member names.
         => kindOrLegacyMember switch
         {
            AlertKinds.ActivityReview or "ActivityReviewWarning" => Strings.Label_NotifyActivityReview,
            AlertKinds.PasswordUpdateReminder or "PasswordUpdateReminderWarning" => Strings.Label_NotifyPasswordUpdateReminder,
            AlertKinds.DuplicatedPasswords or "DuplicatedPasswordsWarning" => Strings.Label_NotifyDuplicatedPasswords,
            AlertKinds.PasswordLeaked or "PasswordLeakedWarning" => Strings.Label_NotifyPasswordLeaked,
            AlertKinds.VaultSecuritySettings or AlertKinds.HostSecuritySettings
               or "SecuritySettingsWarning" => Strings.Label_NotifySecuritySettings,
            AlertKinds.InsufficientPasskeys => Strings.Label_NotifyInsufficientPasskeys,
            AlertKinds.WeakPasskey => Strings.Label_NotifyWeakPasskey,
            AlertKinds.PasskeyLeaked => Strings.Label_NotifyPasskeyLeaked,
            AlertKinds.WeakAccountPassword => Strings.Label_NotifyWeakAccountPassword,
            AlertKinds.PasskeyReusedAsAccountPassword => Strings.Label_NotifyPasskeyReusedAsAccountPassword,
            _ => Strings.Get($"{WARNING_TYPE_PREFIX}{kindOrLegacyMember}"),
         };
#pragma warning restore CS0618
   }
}
