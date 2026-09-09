using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Maps activity/warning enums to localized strings in <c>Strings.resx</c>
   /// (<c>EnumValue_{EnumType}_{member}</c> keys).
   /// These are the short labels for filters / the Event type column.
   /// Full Message sentences use the parallel <c>Activity_*</c> keys — see Wiki WPF Client Localization.
   /// </summary>
   internal static class EnumHelper
   {
      private const string ACTIVITY_EVENT_TYPE_PREFIX = "EnumValue_ActivityEventType_";

      /// <summary>
      /// Sentinel for the account-passwords filter meaning both
      /// <see cref="WarningKinds.PasswordUpdateReminder"/> and
      /// <see cref="WarningKinds.PasswordLeaked"/>.
      /// </summary>
      public const string AccountPasswordFilterAll = "";

      public static string ToReadableString(this ActivityEventType eventType)
      {
         return eventType == ActivityEventType.None ? Strings.Filter_All : Strings.Get($"{ACTIVITY_EVENT_TYPE_PREFIX}{eventType}");
      }

      public static ActivityEventType ActivityEventTypeFromReadableString(string readableString)
      {
         if (readableString == Strings.Filter_All)
         {
            return ActivityEventType.None;
         }

         try
         {
            return Enum.GetValues<ActivityEventType>().First(x => x != ActivityEventType.None && x.ToReadableString() == readableString);
         }
         catch (Exception ex)
            when (ex is InvalidOperationException
            or ArgumentNullException)
         {
            throw new InvalidOperationException($"'{readableString}' event type not handled");
         }
      }

      public static string ToReadableWarningKind(string? kind)
      {
         if (IsAccountPasswordFilterAll(kind))
         {
            return Strings.Filter_All;
         }

         return kind! switch
         {
            WarningKinds.PasswordUpdateReminder => Strings.Get("EnumValue_WarningType_PasswordUpdateReminderWarning"),
            WarningKinds.PasswordLeaked => Strings.Get("EnumValue_WarningType_PasswordLeakedWarning"),
            WarningKinds.WeakAccountPassword => Strings.Label_NotifyWeakAccountPassword,
            WarningKinds.PasskeyReusedAsAccountPassword => Strings.Label_NotifyPasskeyReusedAsAccountPassword,
            WarningKinds.ActivityReview => Strings.Label_NotifyActivityReview,
            WarningKinds.DuplicatedPasswords => Strings.Label_NotifyDuplicatedPasswords,
            WarningKinds.VaultSecuritySettings or WarningKinds.HostSecuritySettings => Strings.Label_NotifySecuritySettings,
            WarningKinds.InsufficientPasskeys => Strings.Label_NotifyInsufficientPasskeys,
            WarningKinds.WeakPasskey => Strings.Label_NotifyWeakPasskey,
            WarningKinds.PasskeyLeaked => Strings.Label_NotifyPasskeyLeaked,
            _ => kind!,
         };
      }

      public static string AccountPasswordKindFromReadableString(string readableString)
      {
         if (readableString == Strings.Filter_All)
         {
            return AccountPasswordFilterAll;
         }

         string reminder = ToReadableWarningKind(WarningKinds.PasswordUpdateReminder);
         string leaked = ToReadableWarningKind(WarningKinds.PasswordLeaked);
         string weak = ToReadableWarningKind(WarningKinds.WeakAccountPassword);
         string reused = ToReadableWarningKind(WarningKinds.PasskeyReusedAsAccountPassword);

         if (readableString == reminder)
         {
            return WarningKinds.PasswordUpdateReminder;
         }

         if (readableString == leaked)
         {
            return WarningKinds.PasswordLeaked;
         }

         if (readableString == weak)
         {
            return WarningKinds.WeakAccountPassword;
         }

         if (readableString == reused)
         {
            return WarningKinds.PasskeyReusedAsAccountPassword;
         }

         throw new InvalidOperationException($"'{readableString}' warning kind not handled");
      }

      public static bool IsAccountPasswordFilterAll(string? kind)
         => string.IsNullOrEmpty(kind);

      public static bool MatchesAccountPasswordKindFilter(string warningKind, string? filterKind)
      {
         if (IsAccountPasswordFilterAll(filterKind))
         {
            return warningKind is WarningKinds.PasswordUpdateReminder or WarningKinds.PasswordLeaked;
         }

         return string.Equals(warningKind, filterKind, StringComparison.Ordinal);
      }
   }
}
