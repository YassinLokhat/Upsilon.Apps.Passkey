using System;
using System.Collections.Generic;
using System.Text;

namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   public enum LogEventType
   {
      None = 0,
      Update = 1,
      Add = 2,
      Delete = 3,

      MergeAndSaveThenRemoveAutoSaveFile = 10,
      MergeWithoutSavingAndKeepAutoSaveFile = 11,
      DontMergeAndRemoveAutoSaveFile = 12,
      DontMergeAndKeepAutoSaveFile = 13,

      DatabaseCreated = 90,
      DatabaseOpened,
      DatabaseSaved,
      DatabaseClosed,
      LoginSessionTimeoutReached,
      LoginFailed,
      UserLoggedIn,
      UserLoggedOut,
      ImportingDataStarted,
      ImportingDataSucceded,
      ImportingDataFailed,
      ExportingDataStarted,
      ExportingDataSucceded,
      ExportingDataFailed,
   }
}
