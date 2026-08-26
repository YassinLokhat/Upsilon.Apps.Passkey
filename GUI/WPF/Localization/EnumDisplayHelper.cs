using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// Localizes enum values stored in activity <c>FieldValue</c> (Core persists
   /// <see cref="Enum.ToString()"/> names, not translated text).
   /// </summary>
   internal static class EnumDisplayHelper
   {
      public static string FormatFieldValue(string? fieldName, string? fieldValue)
      {
         if (string.IsNullOrWhiteSpace(fieldValue))
         {
            return fieldValue ?? string.Empty;
         }

         return fieldName switch
         {
            nameof(AccountOption) or "Options" => _formatAccountOption(fieldValue),
            nameof(WarningType) or "WarningsToNotify" => _formatWarningType(fieldValue),
            _ => fieldValue,
         };
      }

      private static string _formatAccountOption(string stored)
      {
         if (stored is "None" or "0")
         {
            return Strings.EnumValue_AccountOption_None;
         }

         return _formatFlags(stored, _accountOptionLabel);
      }

      private static string _formatWarningType(string stored)
      {
         if (stored is "None" or "0")
         {
            return Strings.EnumValue_WarningType_None;
         }

         return _formatFlags(stored, _warningTypeLabel);
      }

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
            nameof(AccountOption.None) => Strings.EnumValue_AccountOption_None,
            nameof(AccountOption.WarnIfPasswordLeaked) => Strings.Label_WarnPasswordLeak,
            nameof(AccountOption.WarnIfDuplicatedPassword) => Strings.Label_WarnDuplicatedPassword,
            _ => memberName,
         };

      private static string _warningTypeLabel(string memberName)
         => memberName switch
         {
            nameof(WarningType.ActivityReviewWarning) => Strings.Label_NotifyActivityReview,
            nameof(WarningType.PasswordUpdateReminderWarning) => Strings.Label_NotifyPasswordUpdateReminder,
            nameof(WarningType.DuplicatedPasswordsWarning) => Strings.Label_NotifyDuplicatedPasswords,
            nameof(WarningType.PasswordLeakedWarning) => Strings.Label_NotifyPasswordLeaked,
            _ => memberName,
         };
   }
}
