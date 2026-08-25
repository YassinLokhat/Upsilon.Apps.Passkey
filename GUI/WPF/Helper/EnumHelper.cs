using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Maps activity/warning enums to the strings shown in GUI filters.
   /// <see cref="ActivityEventType.None"/> displays as localized "All".
   /// </summary>
   internal static class EnumHelper
   {
      public static string ToReadableString(this ActivityEventType eventType)
      {
         return eventType switch
         {
            ActivityEventType.None => Strings.Filter_All,
            ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile => Strings.Filter_AutoSaveMergedThenSaved,
            ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile => Strings.Filter_AutoSaveMergedNotSaved,
            ActivityEventType.DontMergeAndRemoveAutoSaveFile => Strings.Filter_AutoSaveDiscarded,
            ActivityEventType.DontMergeAndKeepAutoSaveFile => Strings.Filter_AutoSaveNotMergedKept,
            ActivityEventType.DatabaseCreated => Strings.Event_DatabaseCreated,
            ActivityEventType.DatabaseOpened => Strings.Event_DatabaseOpened,
            ActivityEventType.DatabaseSaved => Strings.Event_DatabaseSaved,
            ActivityEventType.DatabaseClosed => Strings.Event_DatabaseClosed,
            ActivityEventType.LoginSessionTimeoutReached => Strings.Event_LoginSessionTimeoutReached,
            ActivityEventType.LoginFailed => Strings.Event_LoginFailed,
            ActivityEventType.UserLoggedIn => Strings.Event_UserLoggedIn,
            ActivityEventType.UserLoggedOut => Strings.Event_UserLoggedOut,
            ActivityEventType.ImportingDataStarted => Strings.Event_ImportingDataStarted,
            ActivityEventType.ImportingDataSucceded => Strings.Event_ImportingDataSucceded,
            ActivityEventType.ImportingDataFailed => Strings.Event_ImportingDataFailed,
            ActivityEventType.ExportingDataStarted => Strings.Event_ExportingDataStarted,
            ActivityEventType.ExportingDataSucceded => Strings.Event_ExportingDataSucceded,
            ActivityEventType.ExportingDataFailed => Strings.Event_ExportingDataFailed,
            ActivityEventType.ItemUpdated => Strings.Event_ItemUpdated,
            ActivityEventType.ItemAdded => Strings.Event_ItemAdded,
            ActivityEventType.ItemDeleted => Strings.Event_ItemDeleted,
            ActivityEventType.ActivityLogTampered => Strings.Event_ActivityLogTampered,
            _ => throw new InvalidOperationException($"'{eventType}' event type not handled"),
         };
      }

      public static ActivityEventType ActivityEventTypeFromReadableString(string readableString)
         => Enum.GetValues<ActivityEventType>()
            .Cast<ActivityEventType>()
            .First(x => x.ToReadableString() == readableString);

      public static string ToReadableString(this WarningType warningType)
      {
         return warningType switch
         {
            WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning => Strings.Filter_All,
            WarningType.PasswordUpdateReminderWarning => Strings.Filter_ExpiredPasswords,
            WarningType.PasswordLeakedWarning => Strings.Filter_LeakedPasswords,
            _ => throw new InvalidOperationException($"'{warningType}' warning type not handled"),
         };
      }

      public static WarningType ActivityWarningTypeFromReadableString(string readableString)
      {
         return readableString == Strings.Filter_All
            ? WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning
            : readableString == Strings.Filter_ExpiredPasswords
            ? WarningType.PasswordUpdateReminderWarning
            : readableString == Strings.Filter_LeakedPasswords
            ? WarningType.PasswordLeakedWarning
            : throw new InvalidOperationException($"'{readableString}' warning type not handled");
      }
   }
}
