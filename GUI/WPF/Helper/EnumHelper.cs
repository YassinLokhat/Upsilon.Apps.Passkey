using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Maps activity/warning enums to localized strings in <c>Strings.resx</c>
   /// (<c>EnumValue_{EnumType}_{member}</c> keys).
   /// </summary>
   internal static class EnumHelper
   {
      private const string ACTIVITY_EVENT_TYPE_PREFIX = "EnumValue_ActivityEventType_";
      private const string WARNING_TYPE_PREFIX = "EnumValue_WarningType_";

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

      public static string ToReadableString(this WarningType warningType)
      {
         return warningType == (WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning)
            ? Strings.Filter_All
            : Strings.Get($"{WARNING_TYPE_PREFIX}{warningType}");
      }

      public static WarningType ActivityWarningTypeFromReadableString(string readableString)
      {
         if (readableString == Strings.Filter_All)
         {
            return WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning;
         }

         try
         {
            return Enum.GetValues<WarningType>().First(x => x.ToReadableString() == readableString);
         }
         catch (Exception ex)
            when (ex is InvalidOperationException
            or ArgumentNullException)
         {
            throw new InvalidOperationException($"'{readableString}' warning type not handled");
         }
      }
   }
}
