using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.UnitTests.Multiplateform
{
   /// <summary>
   /// Structured expectation for an <see cref="Interfaces.Models.IActivity"/> row.
   /// Vault tests compare event type, review flag, and payload fields — not localized UI text.
   /// </summary>
   public sealed record ExpectedActivity(
      ActivityEventType EventType,
      bool NeedsReview,
      string? Username = null,
      string? ServiceName = null,
      string? AccountName = null,
      string? FieldName = null,
      string? FieldValue = null,
      string? ParentName = null)
   {
      public static ExpectedActivity DatabaseSaved(string username)
         => new(ActivityEventType.DatabaseSaved, false, Username: username);

      public static ExpectedActivity DatabaseOpened(string username)
         => new(ActivityEventType.DatabaseOpened, false, Username: username);

      public static ExpectedActivity DatabaseClosed(string username)
         => new(ActivityEventType.DatabaseClosed, false, Username: username);

      public static ExpectedActivity DatabaseCreated(string username)
         => new(ActivityEventType.DatabaseCreated, false, Username: username);

      public static ExpectedActivity UserLoggedIn(string username)
         => new(ActivityEventType.UserLoggedIn, false, Username: username);

      /// <param name="withoutSaving">
      /// When true, Core sets NeedsReview and FieldValue "1"; when false, FieldValue is empty
      /// (null after round-trip). FieldName is always <c>needsReview</c>.
      /// </param>
      public static ExpectedActivity UserLoggedOut(string username, bool withoutSaving = false)
         => new(ActivityEventType.UserLoggedOut,
            withoutSaving,
            Username: username,
            FieldName: "needsReview",
            FieldValue: withoutSaving ? "1" : string.Empty);

      public static ExpectedActivity ImportStarted(string username, string filePath)
         => new(ActivityEventType.ImportingDataStarted, true, Username: username, FieldName: "filePath", FieldValue: filePath);

      public static ExpectedActivity ImportSucceeded(string username)
         => new(ActivityEventType.ImportingDataSucceeded, true, Username: username);

      public static ExpectedActivity ImportFailed(string username, ImportExportError error)
         => new(ActivityEventType.ImportingDataFailed, true, Username: username, FieldName: nameof(ImportExportError), FieldValue: error.ToString());

      public static ExpectedActivity ExportStarted(string username, string filePath)
         => new(ActivityEventType.ExportingDataStarted, true, Username: username, FieldName: "filePath", FieldValue: filePath);

      public static ExpectedActivity ExportSucceeded(string username)
         => new(ActivityEventType.ExportingDataSucceeded, true, Username: username);

      public static ExpectedActivity ExportFailed(string username, ImportExportError error)
         => new(ActivityEventType.ExportingDataFailed, true, Username: username, FieldName: nameof(ImportExportError), FieldValue: error.ToString());

      public static ExpectedActivity ItemAdded(
         bool needsReview,
         string? username = null,
         string? serviceName = null,
         string? fieldValue = null)
         => new(ActivityEventType.ItemAdded, needsReview, Username: username, ServiceName: serviceName, FieldValue: fieldValue);

      public static ExpectedActivity ItemDeleted(
         bool needsReview,
         string? username = null,
         string? serviceName = null,
         string? fieldValue = null)
         => new(ActivityEventType.ItemDeleted, needsReview, Username: username, ServiceName: serviceName, FieldValue: fieldValue);

      public static ExpectedActivity ItemUpdated(
         bool needsReview,
         string? username = null,
         string? serviceName = null,
         string? accountName = null,
         string? fieldName = null,
         string? fieldValue = null,
         string? parentName = null)
         => new(ActivityEventType.ItemUpdated,
            needsReview,
            Username: username,
            ServiceName: serviceName,
            AccountName: accountName,
            FieldName: fieldName,
            FieldValue: fieldValue,
            ParentName: parentName);

      public static ExpectedActivity AutosaveMerged(string username, ActivityEventType eventType)
         => new(eventType, true, Username: username);

      public static ExpectedActivity LoginFailed(string username, string remainingAttempts)
         => new(ActivityEventType.LoginFailed, true, Username: username, FieldName: "PasswordLevel", FieldValue: remainingAttempts);

      public static ExpectedActivity ActivityLogTampered(string username)
         => new(ActivityEventType.ActivityLogTampered, true, Username: username);

      public static ExpectedActivity LoginSessionTimeoutReached(string username)
         => new(ActivityEventType.LoginSessionTimeoutReached, true, Username: username);
   }
}
