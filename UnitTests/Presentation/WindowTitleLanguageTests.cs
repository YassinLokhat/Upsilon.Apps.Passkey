using FluentAssertions;
using UnitTests.Presentation.Fakes;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Activities;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools;

namespace UnitTests.Presentation
{
   /// <summary>
   /// Window titles are ViewModel snapshots (not <c>{loc:Loc}</c>); each
   /// <see cref="ILanguageRefreshable.OnLanguageChanged"/> must rebuild them.
   /// Delivery to open WPF windows is covered by the WPF host tests.
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
         ViewModelTestServices.Install();
         LocalizationService.DetectSystemLanguageCode = static () => LocalizationService.DefaultLanguageCode;
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode, forceRefresh: true);
      }

      [TestCleanup]
      public void Cleanup()
      {
         LocalizationService.DetectSystemLanguageCode = static () => LocalizationService.DefaultLanguageCode;
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode, forceRefresh: true);
         ViewModelTestServices.Reset();
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
         using UserSettingsViewModel userSettings = new();
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
            Strings.Format(nameof(Strings.Title_SecuritySettingsAlertsWindow), AppServices.Info.Title));

         using PasskeyQualityAlertViewModel passkey = new();
         passkey.OnLanguageChanged();
         _ = passkey.Title.Should().Be(
            Strings.Format(nameof(Strings.Title_PasskeyQualityAlertsWindow), AppServices.Info.Title));

         AppSettingsViewModel appSettings = new();
         appSettings.OnLanguageChanged();
         _ = appSettings.Title.Should().Be(
            Strings.Format(nameof(Strings.Title_AppSettings), AppServices.Info.Title));

         UserActivitiesViewModel activities = new();
         activities.OnLanguageChanged();
         _ = activities.Title.Should().Be(
            Strings.Format(nameof(Strings.Title_Activities), AppServices.Info.Title));

         PasswordGeneratorViewModel passwordGenerator = new();
         passwordGenerator.OnLanguageChanged();
         _ = passwordGenerator.Title.Should().Be(
            Strings.Format(nameof(Strings.Title_PasswordGenerator), AppServices.Info.Title));

         CredentialsConfirmationViewModel credentialsNew = new(["u"], isNew: true);
         credentialsNew.OnLanguageChanged();
         _ = credentialsNew.Title.Should().Be(Strings.Title_NewCredentialsConfirmation);

         CredentialsConfirmationViewModel credentialsOld = new(["u"], isNew: false);
         credentialsOld.OnLanguageChanged();
         _ = credentialsOld.Title.Should().Be(Strings.Title_ActualCredentialsConfirmation);
      }
   }
}
