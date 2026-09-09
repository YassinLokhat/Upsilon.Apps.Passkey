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
      /// <see cref="AlertKinds.PasswordUpdateReminder"/> and
      /// <see cref="AlertKinds.PasswordLeaked"/>.
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

      public static string ToReadableAlertKind(string? kind)
      {
         if (IsAccountPasswordFilterAll(kind))
         {
            return Strings.Filter_All;
         }

         return kind! switch
         {
            AlertKinds.PasswordUpdateReminder => Strings.Get("EnumValue_WarningType_PasswordUpdateReminderWarning"),
            AlertKinds.PasswordLeaked => Strings.Get("EnumValue_WarningType_PasswordLeakedWarning"),
            AlertKinds.WeakAccountPassword => Strings.Label_NotifyWeakAccountPassword,
            AlertKinds.PasskeyReusedAsAccountPassword => Strings.Label_NotifyPasskeyReusedAsAccountPassword,
            AlertKinds.ActivityReview => Strings.Label_NotifyActivityReview,
            AlertKinds.DuplicatedPasswords => Strings.Label_NotifyDuplicatedPasswords,
            AlertKinds.VaultSecuritySettings or AlertKinds.HostSecuritySettings => Strings.Label_NotifySecuritySettings,
            AlertKinds.InsufficientPasskeys => Strings.Label_NotifyInsufficientPasskeys,
            AlertKinds.WeakPasskey => Strings.Label_NotifyWeakPasskey,
            AlertKinds.PasskeyLeaked => Strings.Label_NotifyPasskeyLeaked,
            _ => kind!,
         };
      }

      public static string AccountPasswordKindFromReadableString(string readableString)
      {
         if (readableString == Strings.Filter_All)
         {
            return AccountPasswordFilterAll;
         }

         string reminder = ToReadableAlertKind(AlertKinds.PasswordUpdateReminder);
         string leaked = ToReadableAlertKind(AlertKinds.PasswordLeaked);
         string weak = ToReadableAlertKind(AlertKinds.WeakAccountPassword);
         string reused = ToReadableAlertKind(AlertKinds.PasskeyReusedAsAccountPassword);

         if (readableString == reminder)
         {
            return AlertKinds.PasswordUpdateReminder;
         }

         if (readableString == leaked)
         {
            return AlertKinds.PasswordLeaked;
         }

         if (readableString == weak)
         {
            return AlertKinds.WeakAccountPassword;
         }

         if (readableString == reused)
         {
            return AlertKinds.PasskeyReusedAsAccountPassword;
         }

         throw new InvalidOperationException($"'{readableString}' warning kind not handled");
      }

      public static bool IsAccountPasswordFilterAll(string? kind)
         => string.IsNullOrEmpty(kind);

      public static bool MatchesAccountPasswordKindFilter(string warningKind, string? filterKind)
      {
         if (IsAccountPasswordFilterAll(filterKind))
         {
            return warningKind is AlertKinds.PasswordUpdateReminder or AlertKinds.PasswordLeaked;
         }

         return string.Equals(warningKind, filterKind, StringComparison.Ordinal);
      }
   }
}
