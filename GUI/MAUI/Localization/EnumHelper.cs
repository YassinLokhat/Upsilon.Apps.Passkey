using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Localization
{
   internal static class EnumHelper
   {
      private const string ACTIVITY_EVENT_TYPE_PREFIX = "EnumValue_ActivityEventType_";

      public static string ToReadableString(this ActivityEventType eventType)
         => eventType == ActivityEventType.None
            ? Strings.Filter_All
            : Strings.Get($"{ACTIVITY_EVENT_TYPE_PREFIX}{eventType}");

      public static ActivityEventType ActivityEventTypeFromReadableString(string readableString)
      {
         if (readableString == Strings.Filter_All)
         {
            return ActivityEventType.None;
         }

         foreach (ActivityEventType candidate in Enum.GetValues<ActivityEventType>())
         {
            if (candidate != ActivityEventType.None && candidate.ToReadableString() == readableString)
            {
               return candidate;
            }
         }

         throw new InvalidOperationException($"'{readableString}' event type not handled");
      }
   }
}
