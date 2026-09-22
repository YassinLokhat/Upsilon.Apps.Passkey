using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.UnitTests.Windows.Helpers;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   [TestClass]
   public sealed class ActivityViewModelMessageTests
   {
      [TestMethod]
      /*
       * Every activity event type produces a non-empty human-readable message
       * through the WPF localization path.
      */
      public void Case01_Message_CoversEveryEventType()
      {
         Dictionary<ActivityEventType, string?[]> payloads = new()
         {
            [ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile] = ["alice", null, null, null, null, null],
            [ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile] = ["alice", null, null, null, null, null],
            [ActivityEventType.DontMergeAndRemoveAutoSaveFile] = ["alice", null, null, null, null, null],
            [ActivityEventType.DontMergeAndKeepAutoSaveFile] = ["alice", null, null, null, null, null],
            [ActivityEventType.DatabaseCreated] = ["alice", null, null, null, null, null],
            [ActivityEventType.DatabaseOpened] = ["alice", null, null, null, null, null],
            [ActivityEventType.DatabaseSaved] = ["alice", null, null, null, null, null],
            [ActivityEventType.DatabaseClosed] = ["alice", null, null, null, null, null],
            [ActivityEventType.LoginSessionTimeoutReached] = ["alice", null, null, null, null, null],
            [ActivityEventType.LoginFailed] = ["alice", null, null, null, "2", null],
            [ActivityEventType.UserLoggedIn] = ["alice", null, null, null, null, null],
            [ActivityEventType.UserLoggedOut] = ["alice", null, null, null, null, null],
            [ActivityEventType.ImportingDataStarted] = [null, null, null, null, "vault.json", null],
            [ActivityEventType.ImportingDataSucceeded] = [null, null, null, null, null, null],
            [ActivityEventType.ImportingDataFailed] = [null, null, null, nameof(ImportExportError), nameof(ImportExportError.IncorrectCSVFormat), null],
            [ActivityEventType.ExportingDataStarted] = [null, null, null, null, "vault.csv", null],
            [ActivityEventType.ExportingDataSucceeded] = [null, null, null, null, null, null],
            [ActivityEventType.ExportingDataFailed] = [null, null, null, nameof(ImportExportError), nameof(ImportExportError.ExportFileAlreadyExists), null],
            [ActivityEventType.ItemUpdated] = [null, null, "Account", "Notes", "hello", "Service X"],
            [ActivityEventType.ItemAdded] = ["User alice", null, null, null, "Service X", null],
            [ActivityEventType.ItemDeleted] = ["User alice", null, null, null, "Service X", null],
            [ActivityEventType.ActivityLogTampered] = ["alice", null, null, null, null, null],
            [ActivityEventType.None] = [null, null, null, null, "fallback", null],
         };

         foreach (ActivityEventType eventType in Enum.GetValues<ActivityEventType>())
         {
            string?[] data = payloads[eventType];
            ActivityViewModel activity = new(new Activity(DateTime.Now.Ticks, "id", data[0], data[1], data[2], data[3], data[4], data[5], eventType, needsReview: false));

            _ = activity.Message.Should().NotBeNullOrWhiteSpace($"event {eventType} must render a message");
         }

         ActivityViewModel importFailed = new(new Activity(
            DateTime.Now.Ticks,
            "id",
            username: null,
            serviceName: null,
            accountName: null,
            fieldName: nameof(ImportExportError),
            fieldValue: nameof(ImportExportError.IncorrectCSVFormat),
            parentName: null,
            ActivityEventType.ImportingDataFailed,
            needsReview: true));
         _ = importFailed.Message.Should().Be(ActivityAssertHelper.FormatImportFailed(ImportExportError.IncorrectCSVFormat).Split(" : ", 2)[1]);

         ActivityViewModel exportFailed = new(new Activity(
            DateTime.Now.Ticks,
            "id",
            username: null,
            serviceName: null,
            accountName: null,
            fieldName: nameof(ImportExportError),
            fieldValue: nameof(ImportExportError.ExportFileAlreadyExists),
            parentName: null,
            ActivityEventType.ExportingDataFailed,
            needsReview: true));
         _ = exportFailed.Message.Should().Be(ActivityAssertHelper.FormatExportFailed(ImportExportError.ExportFileAlreadyExists).Split(" : ", 2)[1]);

         ActivityViewModel loggedOutDirty = new(new Activity(DateTime.Now.Ticks, "id", "alice", null, null, "needsReview", "1", null, ActivityEventType.UserLoggedOut, needsReview: true));
         _ = loggedOutDirty.Message.Should().Contain("without saving");

         ActivityViewModel updatedBlank = new(new Activity(DateTime.Now.Ticks, "id", null, null, "Account", "Notes", null, null, ActivityEventType.ItemUpdated, needsReview: false));
         _ = updatedBlank.Message.Should().Contain("updated");
         _ = updatedBlank.Message.Should().NotContain("set to");
      }
   }
}
