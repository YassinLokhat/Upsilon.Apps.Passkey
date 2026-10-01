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
   }
}
