using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Activities;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace UnitTests.Presentation
{
   [TestClass]
   [DoNotParallelize]
   public sealed class ActivitiesAlertsViewModelsTests
   {
      [TestCleanup]
      public void Cleanup() => AppServices.Reset();

      [TestMethod]
      public void Activity_MeetsConditions_AppliesDateTypeReviewAndSearch()
      {
         DateTime today = DateTime.Now.Date;
         ActivityViewModel vm = new(new FakeActivity(today.AddDays(-2), "item-1", ActivityEventType.DatabaseOpened, needsReview: false));
         DateTime noFilter = today.AddDays(1);

         vm.MeetsConditions(noFilter, noFilter, ActivityEventType.None, string.Empty, needsReview: false).Should().BeTrue();
         vm.MeetsConditions(today.AddDays(-1), noFilter, ActivityEventType.None, string.Empty, false).Should().BeFalse();
         vm.MeetsConditions(noFilter, today.AddDays(-3), ActivityEventType.None, string.Empty, false).Should().BeFalse();
         vm.MeetsConditions(noFilter, noFilter, ActivityEventType.DatabaseSaved, string.Empty, false).Should().BeFalse();
         vm.MeetsConditions(noFilter, noFilter, ActivityEventType.None, string.Empty, needsReview: true).Should().BeFalse();
         vm.MeetsConditions(noFilter, noFilter, ActivityEventType.None, "item-1", false).Should().BeTrue();
         vm.MeetsConditions(noFilter, noFilter, ActivityEventType.None, "no-such-text", false).Should().BeFalse();
      }

      [TestMethod]
      public void Activity_NeedsReviewSetter_WritesThrough()
      {
         FakeActivity activity = new(DateTime.Now, "id", ActivityEventType.DatabaseOpened, needsReview: true);
         ActivityViewModel vm = new(activity);

         vm.NeedsReview = false;

         activity.NeedsReview.Should().BeFalse();
         vm.NeedsReviewString.Should().Be(Strings.Label_Reviewed);
      }

      [TestMethod]
      public void UserActivities_WithoutDatabase_IsEmpty_AndOptionsRoundTrip()
      {
         UserActivitiesViewModel vm = new();

         vm.Activities.Should().BeEmpty();
         vm.EventTypeOptions.Should().HaveCount(Enum.GetValues<ActivityEventType>().Length);
         vm.EventTypeOptions[0].Should().Be(Strings.Filter_All);

         vm.ReadableEventType = ActivityEventType.LoginFailed.ToReadableString();
         vm.EventType.Should().Be(ActivityEventType.LoginFailed);

         vm.NeedsReview = true;
         vm.ClearFilters();
         vm.EventType.Should().Be(ActivityEventType.None);
         vm.NeedsReview.Should().BeFalse();
      }

      [TestMethod]
      public void AccountPasswordsAlert_KindOptions_RoundTripThroughReadableKind()
      {
         AccountPasswordsAlertViewModel vm = new();

         vm.KindOptions.Should().HaveCount(5);
         foreach (string option in vm.KindOptions)
         {
            vm.ReadableAlertKind = option;
            vm.ReadableAlertKind.Should().Be(option);
         }

         vm.ClearFilters();
         vm.Kind.Should().Be(EnumHelper.AccountPasswordFilterAll);
         vm.Alerts.Should().BeEmpty();
      }

      [TestMethod]
      public void AccountPasswordKindFilter_AllCoversOnlyAccountPasswordKinds()
      {
         EnumHelper.MatchesAccountPasswordKindFilter(AlertKinds.PasswordLeaked, EnumHelper.AccountPasswordFilterAll).Should().BeTrue();
         EnumHelper.MatchesAccountPasswordKindFilter(AlertKinds.DuplicatedPasswords, EnumHelper.AccountPasswordFilterAll).Should().BeFalse();
         EnumHelper.MatchesAccountPasswordKindFilter(AlertKinds.WeakAccountPassword, AlertKinds.PasswordLeaked).Should().BeFalse();
      }

      [TestMethod]
      public void IssueAlerts_WithoutAlerts_AreEmpty_AndRaiseHostRequestsBeforeClose()
      {
         using SecuritySettingsAlertViewModel vm = new();
         List<string> events = [];
         vm.OpenAppSettingsRequested += (s, e) => events.Add("app");
         vm.OpenUserSettingsRequested += (s, e) => events.Add("user");
         vm.CloseRequested += (s, e) => events.Add("close");

         vm.Issues.Should().BeEmpty();
         vm.OpenAppSettingsCommand.Execute(null);
         vm.OpenUserSettingsCommand.Execute(null);

         events.Should().Equal("app", "close", "user", "close");
      }

      private sealed class FakeActivity(DateTime dateTime, string itemId, ActivityEventType eventType, bool needsReview) : IActivity
      {
         public DateTime DateTime { get; } = dateTime;
         public string ItemId { get; } = itemId;
         public string? Username => "alice";
         public string? ServiceName => null;
         public string? AccountName => null;
         public string? FieldName => null;
         public string? FieldValue => null;
         public string? ParentName => null;
         public ActivityEventType EventType { get; } = eventType;
         public bool NeedsReview { get; set; } = needsReview;
      }
   }
}
