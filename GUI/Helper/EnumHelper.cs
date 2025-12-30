using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.Helper
{
   internal static class EnumHelper
   {
      public static string ToReadableString(this ActivityEventType eventType)
      {
         return eventType switch
         {
            ActivityEventType.None => "All",
            ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile => "Auto-save merged then saved",
            ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile => "Auto-save merged but not saved",
            ActivityEventType.DontMergeAndRemoveAutoSaveFile => "Auto-save discarded",
            ActivityEventType.DontMergeAndKeepAutoSaveFile => "Auto-save not merged and keeped",
            ActivityEventType.DatabaseCreated
              or ActivityEventType.DatabaseOpened
              or ActivityEventType.DatabaseSaved
              or ActivityEventType.DatabaseClosed
              or ActivityEventType.LoginSessionTimeoutReached
              or ActivityEventType.LoginFailed
              or ActivityEventType.UserLoggedIn
              or ActivityEventType.UserLoggedOut
              or ActivityEventType.ImportingDataStarted
              or ActivityEventType.ImportingDataSucceded
              or ActivityEventType.ImportingDataFailed
              or ActivityEventType.ExportingDataStarted
              or ActivityEventType.ExportingDataSucceded
              or ActivityEventType.ExportingDataFailed
              or ActivityEventType.ItemUpdated
              or ActivityEventType.ItemAdded
              or ActivityEventType.ItemDeleted => eventType.ToString().ToSentenceCase(),
            _ => throw new InvalidOperationException($"'{eventType}' event type not handled"),
         };
      }

      public static ActivityEventType ActivityEventTypeFromReadableString(string readableString)
      {
         try
         {
            return Enum.GetValues<ActivityEventType>()
               .Cast<ActivityEventType>()
               .First(x => x.ToReadableString() == readableString);
         }
         catch
         {
            throw new InvalidOperationException($"'{readableString}' event type not handled");
         }
      }
   }
}
