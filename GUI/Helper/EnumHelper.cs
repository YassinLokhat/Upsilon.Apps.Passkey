using System;
using System.Collections.Generic;
using System.Text;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.Helper
{
   internal static class EnumHelper
   {
      public static string ToReadableString(this LogEventType eventType)
      {
            return eventType switch
            {
                LogEventType.None => "All",
                LogEventType.MergeAndSaveThenRemoveAutoSaveFile => "Auto-save merged then saved",
                LogEventType.MergeWithoutSavingAndKeepAutoSaveFile => "Auto-save merged but not saved",
                LogEventType.DontMergeAndRemoveAutoSaveFile => "Auto-save discarded",
                LogEventType.DontMergeAndKeepAutoSaveFile => "Auto-save not merged and keeped",
                LogEventType.DatabaseCreated
                  or LogEventType.DatabaseOpened
                  or LogEventType.DatabaseSaved
                  or LogEventType.DatabaseClosed
                  or LogEventType.LoginSessionTimeoutReached
                  or LogEventType.LoginFailed
                  or LogEventType.UserLoggedIn
                  or LogEventType.UserLoggedOut
                  or LogEventType.ImportingDataStarted
                  or LogEventType.ImportingDataSucceded
                  or LogEventType.ImportingDataFailed
                  or LogEventType.ExportingDataStarted
                  or LogEventType.ExportingDataSucceded
                  or LogEventType.ExportingDataFailed
                  or LogEventType.ItemUpdated
                  or LogEventType.ItemAdded
                  or LogEventType.ItemDeleted => eventType.ToString().ToSentenceCase(),
                _ => throw new InvalidOperationException($"'{eventType}' event type not handled"),
            };
        }

      public static LogEventType LogEventTypeFromReadableString(string readableString)
      {
         try
         {
            return Enum.GetValues<LogEventType>()
               .Cast<LogEventType>()
               .First(x => x.ToReadableString() == readableString);
         }
         catch
         {
            throw new InvalidOperationException($"'{readableString}' event type not handled");
         }
      }
   }
}
