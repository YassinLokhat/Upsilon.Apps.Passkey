namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Activity-log event kinds. Numeric values are a persistence contract
   /// (see <c>Activity.ToString</c>) and must stay stable. Mapping from
   /// <see cref="AutoSaveMergeBehavior"/> is explicit in code — do not rely on
   /// matching ordinals.
   /// </summary>
   public enum ActivityEventType
   {
      None = 0,

      MergeAndSaveThenRemoveAutoSaveFile = 1,
      MergeWithoutSavingAndKeepAutoSaveFile = 2,
      DontMergeAndRemoveAutoSaveFile = 3,
      DontMergeAndKeepAutoSaveFile = 4,

      DatabaseCreated = 10,
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
      ItemUpdated,
      ItemAdded,
      ItemDeleted,

      /// <summary>Integrity check failed on login (possible tampering).</summary>
      ActivityLogTampered,
   }
}
