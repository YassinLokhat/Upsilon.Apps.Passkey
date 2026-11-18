using FluentAssertions;
using System.Reflection;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// The vault window's 500 ms title tick must not read the vault through the
   /// interfaces that reset the idle countdown, or auto-logout never fires.
   /// </summary>
   [TestClass]
   [DoNotParallelize]
   public sealed class SessionTimerTests
   {
      private const string TestUsername = nameof(SessionTimerTests);

      private static readonly MethodInfo _titleTick = typeof(UserServicesViewModel)
         .GetMethod("_refreshWindowTitle", BindingFlags.Instance | BindingFlags.NonPublic)!;

      private IDatabase? _database;

      [TestInitialize]
      public void Initialize()
      {
         _ = Dispatcher.CurrentDispatcher;
         GuiTestServices.Install();

         UnitTestsHelper.ClearTestEnvironment(TestUsername);
         _database = UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray(2), TestUsername);
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
      public void TitleTick_DoesNotResetTheIdleCountdown()
      {
         IUser user = _database!.User!;
         user.Settings.LogoutTimeout = 5;
         _ = user.AddService("Alpha");
         _ = user.AddService("Beta");

         using UserServicesViewModel vm = new("Test");
         vm.RefreshFilters();

         var concrete = (Core.Models.User)user;
         concrete.SessionLeftTime = 120;

         for (int i = 0; i < 10; i++)
         {
            _ = _titleTick.Invoke(vm, null);
         }

         _ = concrete.SessionLeftTime.Should().BeLessThanOrEqualTo(120,
            "the countdown keeps going down while the title refreshes (a vault read would reset it to 300)");
      }

      [TestMethod]
      public void TitleTick_CountsChangedServicesFromItsViewModels()
      {
         IUser user = _database!.User!;
         _ = user.AddService("Alpha");
         _ = user.AddService("Beta");

         using UserServicesViewModel vm = new("Test");
         vm.RefreshFilters();
         _ = _titleTick.Invoke(vm, null);

         int changed = user.Services.Count(static s => s.HasChanged());
         int expected = changed != 0 ? changed : user.HasChanged() ? 1 : 0;
         _ = vm.ChangeCount.Should().Be(expected, "the same count as reading the vault directly");
         _ = vm.ChangeCount.Should().BePositive();
      }
   }
}
