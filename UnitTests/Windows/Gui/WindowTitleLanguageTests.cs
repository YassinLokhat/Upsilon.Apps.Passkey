using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// Window titles are ViewModel snapshots (not <c>{loc:Loc}</c>); each
   /// <see cref="ILanguageAware.OnLanguageChanged"/> must rebuild them.
   /// Open-window delivery is covered by <see cref="LanguageAwareNotifyTests"/>.
   /// </summary>
   [TestClass]
   [DoNotParallelize]
   public sealed class WindowTitleLanguageTests
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
      public void OnLanguageChanged_RebuildsTitle_ForAllTitleBearingViewModels()
      {
         AppLanguage satellite = _satellite();

         using SecuritySettingsAlertViewModel security = new();
         using PasskeyQualityAlertViewModel passkey = new();
         DuplicatedPasswordsAlertViewModel duplicated = new();
         AccountPasswordsAlertViewModel accountPasswords = new();
         AppSettingsViewModel appSettings = new();
         UserSettingsViewModel userSettings = new();
         UserActivitiesViewModel activities = new();
         PasswordGeneratorViewModel passwordGenerator = new();
         CredentialsConfirmationViewModel credentialsNew = new(["u", "p"], isNew: true);
         CredentialsConfirmationViewModel credentialsOld = new(["u", "p"], isNew: false);

         (string Name, Func<string> ReadTitle, Action Refresh)[] surfaces =
         [
            ("SecuritySettings", () => security.Title, () => security.OnLanguageChanged()),
            ("PasskeyQuality", () => passkey.Title, () => passkey.OnLanguageChanged()),
            ("DuplicatedPasswords", () => duplicated.Title, () => duplicated.OnLanguageChanged()),
            ("AccountPasswords", () => accountPasswords.Title, () => accountPasswords.OnLanguageChanged()),
            ("AppSettings", () => appSettings.Title, () => appSettings.OnLanguageChanged()),
            ("UserSettings", () => userSettings.Title, () => userSettings.OnLanguageChanged()),
            ("Activities", () => activities.Title, () => activities.OnLanguageChanged()),
            ("PasswordGenerator", () => passwordGenerator.Title, () => passwordGenerator.OnLanguageChanged()),
            ("CredentialsNew", () => credentialsNew.Title, () => credentialsNew.OnLanguageChanged()),
            ("CredentialsOld", () => credentialsOld.Title, () => credentialsOld.OnLanguageChanged()),
         ];

         foreach ((string name, Func<string> readTitle, Action refresh) in surfaces)
         {
            LocalizationService.Apply(LocalizationService.DefaultLanguageCode, forceRefresh: true);
            refresh();
            string english = readTitle();
            _ = english.Should().NotBeNullOrWhiteSpace(because: name);

            LocalizationService.Apply(satellite.Code, forceRefresh: true);
            refresh();
            string localized = readTitle();

            _ = localized.Should().NotBe(english, because: $"{name} ({satellite.Code})");
            _ = localized.Should().NotBeNullOrWhiteSpace(because: name);
         }
      }

      [TestMethod]
      public void OnLanguageChanged_RebuildsExpectedFormattedTitles()
      {
         AppLanguage satellite = _satellite();
         LocalizationService.Apply(satellite.Code, forceRefresh: true);

         using SecuritySettingsAlertViewModel security = new();
         security.OnLanguageChanged();
         _ = security.Title.Should().Be(
            Strings.Format(nameof(Strings.Title_SecuritySettingsAlertsWindow), AppInfo.Title));

         using PasskeyQualityAlertViewModel passkey = new();
         passkey.OnLanguageChanged();
         _ = passkey.Title.Should().Be(
            Strings.Format(nameof(Strings.Title_PasskeyQualityAlertsWindow), AppInfo.Title));

         AppSettingsViewModel appSettings = new();
         appSettings.OnLanguageChanged();
         _ = appSettings.Title.Should().Be(
            Strings.Format(nameof(Strings.Title_AppSettings), AppInfo.Title));

         UserActivitiesViewModel activities = new();
         activities.OnLanguageChanged();
         _ = activities.Title.Should().Be(
            Strings.Format(nameof(Strings.Title_Activities), AppInfo.Title));

         PasswordGeneratorViewModel passwordGenerator = new();
         passwordGenerator.OnLanguageChanged();
         _ = passwordGenerator.Title.Should().Be(
            Strings.Format(nameof(Strings.Title_PasswordGenerator), AppInfo.Title));

         CredentialsConfirmationViewModel credentialsNew = new(["u"], isNew: true);
         credentialsNew.OnLanguageChanged();
         _ = credentialsNew.Title.Should().Be(Strings.Title_NewCredentialsConfirmation);

         CredentialsConfirmationViewModel credentialsOld = new(["u"], isNew: false);
         credentialsOld.OnLanguageChanged();
         _ = credentialsOld.Title.Should().Be(Strings.Title_OldCredentialsConfirmation);
      }
   }
}
