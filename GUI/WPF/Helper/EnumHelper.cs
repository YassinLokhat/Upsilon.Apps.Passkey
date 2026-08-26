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
         if (eventType == ActivityEventType.None)
         {
            return Strings.Filter_All;
         }

         return Strings.Get($"{ACTIVITY_EVENT_TYPE_PREFIX}{eventType}");
      }

      public static ActivityEventType ActivityEventTypeFromReadableString(string readableString)
      {
         if (readableString == Strings.Filter_All)
         {
            return ActivityEventType.None;
         }

         foreach (ActivityEventType eventType in Enum.GetValues<ActivityEventType>())
         {
            if (eventType != ActivityEventType.None && eventType.ToReadableString() == readableString)
            {
               return eventType;
            }
         }

         throw new InvalidOperationException($"'{readableString}' event type not handled");
      }

      public static string ToReadableString(this WarningType warningType)
      {
         if (warningType == (WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning))
         {
            return Strings.Filter_All;
         }

         return Strings.Get($"{WARNING_TYPE_PREFIX}{warningType}");
      }

      public static WarningType ActivityWarningTypeFromReadableString(string readableString)
      {
         if (readableString == Strings.Filter_All)
         {
            return WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning;
         }

         foreach (WarningType warningType in Enum.GetValues<WarningType>())
         {
            if (warningType.ToReadableString() == readableString)
            {
               return warningType;
            }
         }

         throw new InvalidOperationException($"'{readableString}' warning type not handled");
      }
   }
}
