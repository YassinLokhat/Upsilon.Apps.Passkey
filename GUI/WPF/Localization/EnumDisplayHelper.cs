using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Enums;

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

      public static string FormatFieldValue(string? fieldName, string? fieldValue)
      {
         return string.IsNullOrWhiteSpace(fieldValue)
            ? fieldValue ?? string.Empty
            : fieldName switch
            {
               nameof(AccountOption) or "Options" => _formatAccountOption(fieldValue),
               nameof(WarningType) or "WarningsToNotify" => _formatWarningType(fieldValue),
               "Theme" => _formatTheme(fieldValue),
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
            _ => stored,
         };

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
            _ => Strings.Get($"{WARNING_TYPE_PREFIX}{memberName}"),
         };
   }
}
