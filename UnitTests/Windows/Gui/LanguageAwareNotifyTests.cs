using FluentAssertions;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// Exercises the Window → DataContext language refresh contract used by
   /// <see cref="LocalizationService.Apply"/> (not only direct VM calls).
   /// </summary>
   [STATestClass]
   [DoNotParallelize]
   public sealed class LanguageAwareNotifyTests
   {
      private static AppLanguage _satellite()
         => LocalizationService.Shipped.First(l =>
            !string.Equals(l.Code, LocalizationService.DefaultLanguageCode, StringComparison.OrdinalIgnoreCase));

      [TestInitialize]
      public void Initialize()
      {
         GuiTestServices.Install();
         LocalizationService.DetectSystemLanguageCode = static () => LocalizationService.DefaultLanguageCode;
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode, forceRefresh: true);
      }

      [TestCleanup]
      public void Cleanup()
      {
         LocalizationService.DetectSystemLanguageCode = static () => LocalizationService.DefaultLanguageCode;
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode, forceRefresh: true);
         GuiTestServices.Reset();
      }

      [TestMethod]
      public void Notify_WindowWithoutILanguageAware_RefreshesDataContextTitle()
      {
         AppLanguage satellite = _satellite();
         UserSettingsViewModel vm = new();
         Window window = new() { DataContext = vm };

         try
         {
            LocalizationService.Apply(LocalizationService.DefaultLanguageCode, forceRefresh: true);
            vm.OnLanguageChanged();
            string english = vm.Title;

            LocalizationService.Apply(satellite.Code, forceRefresh: true);
            LanguageAwareNotify.Notify(window);

            _ = vm.Title.Should().NotBe(english);
            _ = vm.Languages[0].NativeName.Should().Be(Strings.Label_UseAppLanguage);
         }
         finally
         {
            window.Close();
         }
      }

      [TestMethod]
      public void Notify_WindowILanguageAware_ForwardsToDataContext_WithoutDoubleCallFromLegacyPath()
      {
         CountingLanguageAwareWindow window = new();
         try
         {
            LanguageAwareNotify.Notify(window);
            _ = window.WindowCalls.Should().Be(1);
            _ = window.ViewModelCalls.Should().Be(1);
         }
         finally
         {
            window.Close();
         }
      }

      [TestMethod]
      public void Notify_ThrowingWindow_DoesNotPropagate()
      {
         Window window = new ThrowingLanguageAwareWindow();
         try
         {
            Action act = () => LanguageAwareNotify.Notify(window);
            _ = act.Should().NotThrow();
         }
         finally
         {
            window.Close();
         }
      }

      private sealed class CountingViewModel : ILanguageAware
      {
         public int Calls;

         public void OnLanguageChanged() => Calls++;
      }

      private sealed class CountingLanguageAwareWindow : Window, ILanguageAware
      {
         private readonly CountingViewModel _vm = new();

         public int WindowCalls { get; private set; }
         public int ViewModelCalls => _vm.Calls;

         public CountingLanguageAwareWindow()
         {
            DataContext = _vm;
         }

         public void OnLanguageChanged()
         {
            WindowCalls++;
            this.ForwardToDataContext();
         }
      }

      private sealed class ThrowingLanguageAwareWindow : Window, ILanguageAware
      {
         public void OnLanguageChanged()
            => throw new InvalidOperationException("simulated language refresh failure");
      }
   }
}
