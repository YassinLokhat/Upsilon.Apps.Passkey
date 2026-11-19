using FluentAssertions;
using System.Globalization;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// Settings dialogs preview theme and language as soon as they are picked,
   /// keep them on save and put the previous ones back when closed without saving.
   /// </summary>
   [TestClass]
   [DoNotParallelize]
   public sealed class SettingsPreviewTests
   {
      private string _appTheme = string.Empty;
      private string _appLanguage = string.Empty;
      private CultureInfo _culture = CultureInfo.CurrentUICulture;

      [TestInitialize]
      public void Initialize()
      {
         GuiTestServices.Install();

         // Loads the app config first (as start-up does), so it cannot replace
         // the settings set up below.
         _ = AppInfo.Title;
         _appTheme = AppInfo.AppSettings.Theme;
         _appLanguage = AppInfo.AppSettings.Language;
         _culture = CultureInfo.CurrentUICulture;

         ThemeService.DetectSystemLightTheme = static () => false;
         AppInfo.AppSettings.Theme = ThemeService.DarkCode;
         AppInfo.AppSettings.Language = LocalizationService.DefaultLanguageCode;
         _ = ThemeService.Apply(ThemeService.DarkCode);
         _ = LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
      }

      [TestCleanup]
      public void Cleanup()
      {
         AppInfo.AppSettings.Theme = _appTheme;
         AppInfo.AppSettings.Language = _appLanguage;
         _ = ThemeService.Apply(ThemeService.DarkCode);
         _ = LocalizationService.Apply(_culture.TwoLetterISOLanguageName);
         ThemeService.Shutdown();
         GuiTestServices.Reset();
      }

      [TestMethod]
      public void AppSettings_Theme_PreviewsAtOnce_AndRevertsWithoutSave()
      {
         AppSettingsViewModel vm = new();

         vm.SelectedTheme = ThemeService.GetOptionOrDefault(ThemeService.LightCode);

         _ = ThemeService.CurrentAppearance.Should().Be(ThemeService.LightCode, "the pick is shown before saving");

         vm.EndPreview(saved: false);

         _ = ThemeService.CurrentAppearance.Should().Be(ThemeService.DarkCode);
         _ = AppInfo.AppSettings.Theme.Should().Be(ThemeService.DarkCode, "the unsaved pick is dropped");
      }

      [TestMethod]
      public void AppSettings_Theme_StaysAfterSave()
      {
         AppSettingsViewModel vm = new();

         vm.SelectedTheme = ThemeService.GetOptionOrDefault(ThemeService.LightCode);
         vm.EndPreview(saved: true);

         _ = ThemeService.CurrentAppearance.Should().Be(ThemeService.LightCode);
         _ = AppInfo.AppSettings.Theme.Should().Be(ThemeService.LightCode);
      }

      [TestMethod]
      public void AppSettings_Language_PreviewsAtOnce_AndRevertsWithoutSave()
      {
         AppSettingsViewModel vm = new();

         vm.SelectedLanguage = LocalizationService.GetLanguageOrDefault("fr");

         _ = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Should().Be("fr");

         vm.EndPreview(saved: false);

         _ = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Should().Be(LocalizationService.DefaultLanguageCode);
         _ = AppInfo.AppSettings.Language.Should().Be(LocalizationService.DefaultLanguageCode);
      }

      [TestMethod]
      public void AppSettings_EndPreview_IsIdempotent()
      {
         AppSettingsViewModel vm = new();

         vm.SelectedTheme = ThemeService.GetOptionOrDefault(ThemeService.LightCode);
         vm.EndPreview(saved: true);
         vm.EndPreview(saved: false);

         _ = ThemeService.CurrentAppearance.Should().Be(ThemeService.LightCode, "a second close must not undo a save");
      }

      [TestMethod]
      public void UserSettings_ThemeAndLanguage_PreviewAndRevert()
      {
         UserSettingsViewModel vm = new();

         vm.SelectedTheme = vm.Themes.First(t => t.Code == ThemeService.LightCode);
         vm.SelectedLanguage = vm.Languages.First(l => l.Code == "fr");

         _ = ThemeService.CurrentAppearance.Should().Be(ThemeService.LightCode);
         _ = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Should().Be("fr");

         vm.EndPreview(saved: false);

         _ = ThemeService.CurrentAppearance.Should().Be(ThemeService.DarkCode, "back to the application theme");
         _ = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Should().Be(LocalizationService.DefaultLanguageCode);
      }

      [TestMethod]
      public void UserSettings_FollowAppTheme_PreviewsTheAppTheme()
      {
         AppInfo.AppSettings.Theme = ThemeService.LightCode;
         UserSettingsViewModel vm = new();

         vm.SelectedTheme = vm.Themes.First(t => t.Code == ThemeService.DarkCode);
         _ = ThemeService.CurrentAppearance.Should().Be(ThemeService.DarkCode);

         vm.SelectedTheme = vm.Themes[0]; // "Use application theme"
         _ = ThemeService.CurrentAppearance.Should().Be(ThemeService.LightCode);

         vm.EndPreview(saved: false);
      }
   }
}
