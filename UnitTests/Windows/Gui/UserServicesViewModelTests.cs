using Upsilon.Apps.Passkey.GUI.Presentation.Alerts;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Activities;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts;
using FluentAssertions;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;
using AsyncRelayCommand = Upsilon.Apps.Passkey.GUI.Presentation.Helper.AsyncRelayCommand;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   [TestClass]
   [DoNotParallelize]
   public sealed class UserServicesViewModelTests
   {
      private const string TestUsername = nameof(UserServicesViewModelTests);

      private IDatabase? _database;

      [TestInitialize]
      public void Initialize()
      {
         _ = Dispatcher.CurrentDispatcher;
         GuiTestServices.Install();

         UnitTestsHelper.ClearTestEnvironment(TestUsername);
         string[] passkeys = UnitTestsHelper.GetRandomStringArray(2);
         _database = UnitTestsHelper.CreateTestDatabase(passkeys, TestUsername);
         GuiTestServices.Session.StartSession(_database);
      }

      [TestCleanup]
      public void Cleanup()
      {
         GuiTestServices.Session.EndSession();
         _database = null;
         GuiTestServices.Reset();
         UnitTestsHelper.ClearTestEnvironment(TestUsername);
      }

      [TestMethod]
      public void Case01_RefreshFilters_LoadsServicesFromSessionUser()
      {
         IUser user = _database!.User!;
         _ = user.AddService("Alpha Service");
         _ = user.AddService("Beta Service");

         using UserServicesViewModel vm = new("Test");
         vm.RefreshFilters();

         _ = vm.Services.Select(s => s.ServiceName).Should().Equal("Alpha Service", "Beta Service");
      }

      [TestMethod]
      public void Case02_AddService_InsertsNewServiceAtTop()
      {
         using UserServicesViewModel vm = new("Test");
         vm.RefreshFilters();

         ServiceViewModel added = vm.AddService();

         _ = added.ServiceName.Should().StartWith(Strings.Msg_NewServicePrefix);
         _ = vm.Services.Should().ContainSingle();
         _ = vm.Services[0].Should().BeSameAs(added);
         _ = _database!.User!.Services.Should().ContainSingle(s => s.ItemId == added.Service.ItemId);
      }

      [TestMethod]
      public void Case03_AddService_ReusesExistingNewServicePlaceholder()
      {
         using UserServicesViewModel vm = new("Test");
         vm.RefreshFilters();

         ServiceViewModel first = vm.AddService();
         ServiceViewModel second = vm.AddService();

         _ = second.Should().BeSameAs(first);
         _ = vm.Services.Should().ContainSingle();
         _ = _database!.User!.Services.Should().ContainSingle();
      }

      [TestMethod]
      public void Case04_DeleteService_RemovesFromViewAndUser()
      {
         IService service = _database!.User!.AddService("To Delete");

         using UserServicesViewModel vm = new("Test");
         vm.RefreshFilters();

         ServiceViewModel toDelete = vm.Services.Single();
         int nextIndex = vm.DeleteService(toDelete);

         _ = vm.Services.Should().BeEmpty();
         _ = nextIndex.Should().Be(-1);
         _ = _database.User.Services.Should().NotContain(s => s.ItemId == service.ItemId);
      }

      [TestMethod]
      public async Task Case04b_DeleteServiceCommand_AsksThroughSharedDialogs()
      {
         _ = _database!.User!.AddService("Keep On No");

         using UserServicesViewModel vm = new("Test");
         vm.RefreshFilters();
         vm.SelectedService = vm.Services.Single();

         GuiTestServices.Dialogs.ConfirmResult = System.Windows.MessageBoxResult.No;
         await ((AsyncRelayCommand)vm.DeleteServiceCommand).ExecuteAsync(null);
         _ = vm.Services.Should().ContainSingle();

         GuiTestServices.Dialogs.ConfirmResult = System.Windows.MessageBoxResult.Yes;
         await ((AsyncRelayCommand)vm.DeleteServiceCommand).ExecuteAsync(null);
         _ = vm.Services.Should().BeEmpty();
         _ = GuiTestServices.Dialogs.Confirms.Should().HaveCount(2);
      }

      [TestMethod]
      public void Case05_RefreshFilters_AppliesServiceNameFilter()
      {
         IUser user = _database!.User!;
         _ = user.AddService("Keep Me");
         _ = user.AddService("Drop Me");

         using UserServicesViewModel vm = new("Test");
         vm.ServiceFilter = "Keep";
         vm.RefreshFilters();

         _ = vm.Services.Select(s => s.ServiceName).Should().Equal("Keep Me");
      }

      [TestMethod]
      public void Case06_RefreshFilters_ClearsWhenSessionHasNoUser()
      {
         using UserServicesViewModel vm = new("Test");
         _ = vm.AddService();
         _ = vm.Services.Should().NotBeEmpty();

         GuiTestServices.Session.EndSession(closeDatabase: false);
         _database?.Close();
         _database = null;

         vm.RefreshFilters();

         _ = vm.Services.Should().BeEmpty();
      }

      [TestMethod]
      public void Case07_RefreshFilters_AppliesIdentifierTypeFilter()
      {
         IUser user = _database!.User!;
         IService emailService = user.AddService("Email Service");
         _ = emailService.AddAccount("Mail", [new Identifier(IdentifierType.Email, "a@test.te")]);
         IService userService = user.AddService("User Service");
         _ = userService.AddAccount("User", [new Identifier(IdentifierType.Username, "alice")]);

         using UserServicesViewModel vm = new("Test");
         vm.Type = IdentifierType.Email;
         vm.RefreshFilters();

         _ = vm.Services.Select(s => s.ServiceName).Should().Equal("Email Service");
         _ = vm.TypeChoices.Select(c => c.Display).Should().Equal(
            IdentifierViewModel.AllTypeGlyph,
            "👤", "📧", "🖁", "🗝", "📲");
      }

      [TestMethod]
      public void Case08_ClearFilters_ResetsIdentifierTypeToAll()
      {
         using UserServicesViewModel vm = new("Test");
         vm.Type = IdentifierType.Passkey;
         vm.IdentifierFilter = "x";

         vm.ClearFilters();

         _ = vm.Type.Should().Be(IdentifierViewModel.AllIdentifierType);
         _ = vm.IdentifierFilter.Should().BeEmpty();
         _ = vm.TypeLabel.Should().Be(Strings.IdentifierType_All);
      }

      [TestMethod]
      public void Case08b_SelectedTypeChoice_MirrorsTheIdentifierTypeFilter()
      {
         using UserServicesViewModel vm = new("Test");
         List<string?> changed = [];
         vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

         _ = vm.SelectedTypeChoice.Should().BeSameAs(vm.TypeChoices[0]);
         _ = vm.SelectedTypeChoice.ToString().Should().Be(IdentifierViewModel.AllTypeGlyph);

         vm.SelectedTypeChoice = vm.TypeChoices.Single(c => c.Type == IdentifierType.Email);
         _ = vm.Type.Should().Be(IdentifierType.Email);

         vm.SelectedTypeChoice = null!;
         _ = vm.Type.Should().Be(IdentifierType.Email);

         vm.ClearFilters();
         _ = vm.SelectedTypeChoice.Should().BeSameAs(vm.TypeChoices[0]);
         _ = changed.Should().Contain(nameof(UserServicesViewModel.SelectedTypeChoice));
      }

      [TestMethod]
      public void Case09_OnLanguageChanged_RebuildsSessionLeftTimeTitle()
      {
         _database!.User!.Settings.LogoutTimeout = 5;
         ((User)_database.User).ResetTimer();

         using UserServicesViewModel vm = new("TestUser");
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode, forceRefresh: true);
         vm.OnLanguageChanged();

         string englishSuffix = Strings.Format(nameof(Strings.Msg_SessionLeftTime), 5, 0);
         string englishTitle = vm.Title;
         _ = englishTitle.Should().Contain(englishSuffix);

         AppLanguage satellite = LocalizationService.Shipped.First(l =>
            !string.Equals(l.Code, LocalizationService.DefaultLanguageCode, StringComparison.OrdinalIgnoreCase));
         LocalizationService.Apply(satellite.Code, forceRefresh: true);
         vm.OnLanguageChanged();

         string localizedSuffix = Strings.Format(nameof(Strings.Msg_SessionLeftTime), 5, 0);
         _ = localizedSuffix.Should().NotBe(englishSuffix);
         _ = vm.Title.Should().Contain(localizedSuffix);
         _ = vm.Title.Should().NotBe(englishTitle);

         LocalizationService.Apply(LocalizationService.DefaultLanguageCode, forceRefresh: true);
      }

      [TestMethod]
      public void Case10_UndoRedoCommands_TrackEditHistoryCanExecute()
      {
         using UserServicesViewModel vm = new("Test");
         vm.RefreshFilters();

         _ = vm.UndoCommand.CanExecute(null).Should().BeFalse();
         _ = vm.RedoCommand.CanExecute(null).Should().BeFalse();

         _ = _database!.User!.AddService("Undoable Service");
         RelayCommand.RaiseCanExecuteChanged();

         _ = vm.UndoCommand.CanExecute(null).Should().BeTrue();
         _ = vm.RedoCommand.CanExecute(null).Should().BeFalse();

         vm.UndoCommand.Execute(null);

         _ = _database.User.Services.Should().BeEmpty();
         _ = vm.UndoCommand.CanExecute(null).Should().BeFalse();
         _ = vm.RedoCommand.CanExecute(null).Should().BeTrue();

         vm.RedoCommand.Execute(null);

         _ = _database.User.Services.Should().ContainSingle(s => s.ServiceName == "Undoable Service");
         _ = vm.RedoCommand.CanExecute(null).Should().BeFalse();
      }

      [TestMethod]
      public void Case11_SecuritySettingsAlertViewModel_OnLanguageChanged_RebuildsTitle()
      {
         using SecuritySettingsAlertViewModel vm = new();
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode, forceRefresh: true);
         vm.OnLanguageChanged();
         string englishTitle = vm.Title;

         AppLanguage satellite = LocalizationService.Shipped.First(l =>
            !string.Equals(l.Code, LocalizationService.DefaultLanguageCode, StringComparison.OrdinalIgnoreCase));
         LocalizationService.Apply(satellite.Code, forceRefresh: true);
         vm.OnLanguageChanged();

         _ = vm.Title.Should().NotBe(englishTitle, because: satellite.Code);
         _ = vm.Title.Should().Be(
            Strings.Format(nameof(Strings.Title_SecuritySettingsAlertsWindow), AppInfo.Title),
            because: satellite.Code);

         LocalizationService.Apply(LocalizationService.DefaultLanguageCode, forceRefresh: true);
      }

      [TestMethod]
      public void Case12_TrySelectItem_SelectsServiceAndAccountAfterClearingFilters()
      {
         IService service = _database!.User!.AddService("Target Service");
         IAccount account = service.AddAccount("Target", UnitTestsHelper.Ids("target@test"), "pw-target");
         _ = _database.User.AddService("Other Service");

         using UserServicesViewModel vm = new("Test");
         vm.ServiceFilter = "Other";
         vm.RefreshFilters();

         _ = vm.TrySelectItem(account.ItemId).Should().BeTrue();
         _ = vm.ServiceFilter.Should().BeEmpty();
         _ = vm.SelectedService!.Service.ItemId.Should().Be(service.ItemId);
         _ = vm.SelectedService.SelectedAccount!.Account.ItemId.Should().Be(account.ItemId);

         _ = vm.TrySelectItem(service.ItemId).Should().BeTrue();
         _ = vm.TrySelectItem("S-unknown").Should().BeFalse();
         _ = vm.SelectedService.Should().BeNull();
         _ = vm.TrySelectItem(string.Empty).Should().BeFalse();
      }

      [TestMethod]
      public void Case13_ApplyAlertSummary_ExposesTotalsAndOrderedGroups()
      {
         using UserServicesViewModel vm = new("Test");

         vm.ApplyAlertSummary(AlertSummary.Empty);

         _ = vm.HasAlerts.Should().BeFalse();
         _ = vm.ShowAlerts.Should().Be(Strings.Format(nameof(Strings.Msg_ShowAlerts), 0));
         _ = vm.AlertGroups.Should().Equal(AlertSummary.Empty.OrderedGroups);

         foreach (AlertGroup group in Enum.GetValues<AlertGroup>())
         {
            _ = vm.CommandFor(group).Should().NotBeNull();
         }

         _ = vm.CommandFor(AlertGroup.LeakedPasswords).Should().BeSameAs(vm.ShowLeakedPasswordAlertsCommand);
      }

      [TestMethod]
      public void Case14_CurrentIdentifierAndPassword_FollowSelection()
      {
         IService service = _database!.User!.AddService("Copy Service");
         _ = service.AddAccount("Copy", UnitTestsHelper.Ids("first@test", "second@test"), "pw-copy");

         using UserServicesViewModel vm = new("Test");
         vm.RefreshFilters();

         _ = vm.CurrentIdentifier.Should().BeNull();

         vm.SelectedService = vm.Services.Single();
         AccountViewModel account = vm.SelectedService.Accounts.Single();
         vm.SelectedService.SelectedAccount = account;
         account.LoadIdentifiers();

         _ = vm.CurrentIdentifier.Should().Be("first@test");
         _ = vm.CurrentPassword.Should().Be("pw-copy");

         account.SelectedIdentifier = account.Identifiers[1];
         _ = vm.CurrentIdentifier.Should().Be("second@test");
      }

      [TestMethod]
      public void Case15_LoadIdentifiers_RebuildsRowsAndAddsBlankForEmptyAccount()
      {
         IService service = _database!.User!.AddService("Load Service");
         IAccount filled = service.AddAccount("Filled", UnitTestsHelper.Ids("a@test", "b@test"), "pw-a");
         IAccount empty = service.AddAccount("Empty", UnitTestsHelper.Ids(), "pw-b");

         using AccountViewModel filledVm = new(filled);
         filledVm.LoadIdentifiers();
         filledVm.LoadIdentifiers();
         _ = filledVm.Identifiers.Select(i => i.Identifier).Should().Equal("a@test", "b@test");

         using AccountViewModel emptyVm = new(empty);
         emptyVm.LoadIdentifiers();
         _ = emptyVm.Identifiers.Should().ContainSingle().Which.Identifier.Should().BeEmpty();
      }
   }
}
