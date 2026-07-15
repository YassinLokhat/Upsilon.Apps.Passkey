namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Represent the event type.
   /// </summary>
   public enum ActivityEventType
   {
      /// <summary>
      /// No event type.
      /// </summary>
      /// <remarks>
      /// The numeric values below are a persistence contract: they are written to
      /// the activity log (see <c>Activity.ToString</c>) and must stay stable to
      /// keep existing logs readable. Mapping from <see cref="AutoSaveMergeBehavior"/>
      /// is done explicitly in code, so these values are no longer required to
      /// match that enum.
      /// </remarks>
      None = 0,

      /// <summary>
      /// The auto-save merged and saved then removed.
      /// </summary>
      MergeAndSaveThenRemoveAutoSaveFile = 1,
      /// <summary>
      /// The auto-save merged but not saved.
      /// </summary>
      MergeWithoutSavingAndKeepAutoSaveFile = 2,
      /// <summary>
      /// The auto-save not merged then removed.
      /// </summary>
      DontMergeAndRemoveAutoSaveFile = 3,
      /// <summary>
      /// The auto-save not merged.
      /// </summary>
      DontMergeAndKeepAutoSaveFile = 4,

      /// <summary>
      /// Database created.
      /// </summary>
      DatabaseCreated = 10,
      /// <summary>
      /// Database opened.
      /// </summary>
      DatabaseOpened,
      /// <summary>
      /// Database saved.
      /// </summary>
      DatabaseSaved,
      /// <summary>
      /// Database closed.
      /// </summary>
      DatabaseClosed,
      /// <summary>
      /// Login session timeout reached.
      /// </summary>
      LoginSessionTimeoutReached,
      /// <summary>
      /// Login failed.
      /// </summary>
      LoginFailed,
      /// <summary>
      /// User logged in.
      /// </summary>
      UserLoggedIn,
      /// <summary>
      /// User logged out.
      /// </summary>
      UserLoggedOut,
      /// <summary>
      /// Importing data started.
      /// </summary>
      ImportingDataStarted,
      /// <summary>
      /// Importing data succeded.
      /// </summary>
      ImportingDataSucceded,
      /// <summary>
      /// Importing data failed.
      /// </summary>
      ImportingDataFailed,
      /// <summary>
      /// Exporting data started.
      /// </summary>
      ExportingDataStarted,
      /// <summary>
      /// Exporting data succeded.
      /// </summary>
      ExportingDataSucceded,
      /// <summary>
      /// Exporting data failed.
      /// </summary>
      ExportingDataFailed,
      /// <summary>
      /// Item updated.
      /// </summary>
      ItemUpdated,
      /// <summary>
      /// Item added.
      /// </summary>
      ItemAdded,
      /// <summary>
      /// Item deleted.
      /// </summary>
      ItemDeleted,

      /// <summary>
      /// The activity log failed its integrity check on login (possible tampering).
      /// </summary>
      ActivityLogTampered,
   }
}
