using FluentAssertions;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.UnitTests.Gui
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

         _ = added.ServiceName.Should().StartWith("New Service #");
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
   }
}
