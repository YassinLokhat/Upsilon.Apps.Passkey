using FluentAssertions;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace UnitTests.Presentation
{
   [TestClass]
   [DoNotParallelize]
   public sealed class LocalizationTests
   {
      private static readonly ResourceManager Manager = new(
         "Upsilon.Apps.Passkey.GUI.Presentation.Localization.Strings",
         typeof(Strings).Assembly);

      /// <summary>
      /// Every registered UI language except the System preference and the
      /// neutral English fallback. Adding <c>new("xx", …)</c> to
      /// <see cref="LocalizationService.Shipped"/> automatically includes that
      /// satellite in key-parity and localization checks.
      /// </summary>
      private static IEnumerable<AppLanguage> _satelliteLanguages()
         => LocalizationService.Shipped.Where(l =>
            !string.Equals(l.Code, LocalizationService.DefaultLanguageCode, StringComparison.OrdinalIgnoreCase));

      [TestInitialize]
      public void Initialize()
      {
         LocalizationService.DetectSystemLanguageCode = static () => LocalizationService.DefaultLanguageCode;
         _ = LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
      }

      [TestCleanup]
      public void Cleanup()
      {
         LocalizationService.DetectSystemLanguageCode = static () => LocalizationService.DefaultLanguageCode;
         _ = LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
         AppServices.Reset();
      }

      [TestMethod]
      public void Case01_SupportedLanguages_IncludeSystemDefaultAndAtLeastOneSatellite()
      {
         IReadOnlyList<string> codes = [.. LocalizationService.Supported.Select(l => l.Code)];

         _ = codes.Should().Contain(LocalizationService.SystemCode);
         _ = codes.Should().Contain(LocalizationService.DefaultLanguageCode);
         _ = codes[0].Should().Be(LocalizationService.SystemCode);
         _ = codes.Should().OnlyHaveUniqueItems();
         _ = LocalizationService.Shipped.Select(l => l.Code).Should().NotContain(LocalizationService.SystemCode);
         _ = _satelliteLanguages().Should().NotBeEmpty(
            "at least one translated satellite (e.g. fr) must ship with the clients");
      }

      [TestMethod]
      public void Case02_SatelliteResources_ContainEveryNeutralKey()
      {
         using ResourceSet? neutral = Manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
         _ = neutral.Should().NotBeNull();

         HashSet<string> neutralKeys = neutral!.Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .ToHashSet(StringComparer.Ordinal);

         foreach (AppLanguage language in _satelliteLanguages())
         {
            using ResourceSet? satellite = Manager.GetResourceSet(
               CultureInfo.GetCultureInfo(language.Code),
               createIfNotExists: true,
               tryParents: false);

            _ = satellite.Should().NotBeNull($"missing satellite resources for '{language.Code}'");

            HashSet<string> satelliteKeys = satellite!.Cast<System.Collections.DictionaryEntry>()
               .Select(e => (string)e.Key)
               .ToHashSet(StringComparer.Ordinal);

            _ = satelliteKeys.Should().BeEquivalentTo(
               neutralKeys,
               because: $"'{language.Code}' must have the same keys as the neutral Strings.resx");

            foreach (string key in neutralKeys)
            {
               _ = satellite.GetString(key).Should().NotBeNullOrEmpty(
                  $"'{language.Code}' resource missing or empty for key '{key}'");
            }
         }
      }

      [TestMethod]
      /*
       * Every typed accessor on Strings resolves to a neutral resource (no key
       * typo falling back to the key name).
      */
      public void Case03_TypedAccessors_ResolveToNeutralResources()
      {
         foreach (System.Reflection.PropertyInfo property in typeof(Strings).GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
         {
            _ = Strings.GetNeutral(property.Name).Should().NotBe(property.Name, because: property.Name);
         }
      }

      [TestMethod]
      public void Case04_EnumDisplayHelper_FormatsAccountOptionFlags_InDefaultLanguage()
      {
         _ = EnumDisplayHelper.FormatFieldValue("Options", "None")
            .Should().Be(Strings.EnumValue_None);

         _ = EnumDisplayHelper.FormatFieldValue("Options", nameof(AccountOption.WarnIfPasswordLeaked))
            .Should().Be(Strings.Label_WarnPasswordLeak);

         string combined = EnumDisplayHelper.FormatFieldValue("Options",
            $"{nameof(AccountOption.WarnIfPasswordLeaked)}, {nameof(AccountOption.WarnIfDuplicatedPassword)}, {nameof(AccountOption.WarnIfWeakPassword)}");

         _ = combined.Should().Be(
            $"{Strings.Label_WarnPasswordLeak}, {Strings.Label_WarnDuplicatedPassword}, {Strings.Label_WarnWeakPassword}");
      }

      [TestMethod]
      public void Case05_EnumDisplayHelper_FormatsAlertKinds_InEachSatelliteLanguage()
      {
         foreach (AppLanguage language in _satelliteLanguages())
         {
            _ = LocalizationService.Apply(language.Code);

            _ = EnumDisplayHelper.FormatFieldValue("AlertsToNotify", "None")
               .Should().Be(Strings.EnumValue_None, because: language.Code);

            string combined = EnumDisplayHelper.FormatFieldValue("AlertsToNotify",
               $"{AlertKinds.ActivityReview}, {AlertKinds.PasswordLeaked}");

            _ = combined.Should().Be(
               $"{Strings.Label_NotifyActivityReview}, {Strings.Label_NotifyPasswordLeaked}",
               because: language.Code);
         }
      }

      [TestMethod]
      public void Case06_Apply_RaisesLanguageChanged_OnlyWhenCultureChanges()
      {
         int raised = 0;
         void handler(object? _, EventArgs __) => raised++;
         LocalizationService.LanguageChanged += handler;
         try
         {
            AppLanguage satellite = _satelliteLanguages().First();

            _ = LocalizationService.Apply(LocalizationService.DefaultLanguageCode).Should().BeFalse();
            _ = raised.Should().Be(0);

            _ = LocalizationService.Apply(satellite.Code).Should().BeTrue();
            _ = raised.Should().Be(1);

            _ = LocalizationService.Apply(satellite.Code).Should().BeFalse();
            _ = raised.Should().Be(1);

            _ = LocalizationService.Apply(satellite.Code, forceRefresh: true).Should().BeFalse();
            _ = raised.Should().Be(2);
         }
         finally
         {
            LocalizationService.LanguageChanged -= handler;
         }
      }

      [TestMethod]
      /*
       * The catalog indexer follows the culture, and a change raises
       * PropertyChanged with an empty name (refresh-all for WPF and MAUI) before
       * LanguageChanged, so host listeners see refreshed bindings.
      */
      public void Case07_Catalog_FollowsCulture_AndNotifiesBeforeLanguageChanged()
      {
         string english = LocalizationCatalog.Instance[nameof(Strings.Menu_Save)];
         List<string> order = [];
         void onCatalog(object? _, PropertyChangedEventArgs e) => order.Add($"catalog:{e.PropertyName}");
         void onLanguage(object? _, EventArgs __) => order.Add("language");
         LocalizationCatalog.Instance.PropertyChanged += onCatalog;
         LocalizationService.LanguageChanged += onLanguage;
         try
         {
            foreach (AppLanguage language in _satelliteLanguages())
            {
               order.Clear();
               _ = LocalizationService.Apply(language.Code);

               string localized = LocalizationCatalog.Instance[nameof(Strings.Menu_Save)];
               _ = localized.Should().NotBe(english, because: language.Code);
               _ = localized.Should().Be(Strings.Menu_Save, because: language.Code);
               _ = order.Should().Equal("catalog:", "language");

               _ = LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
            }
         }
         finally
         {
            LocalizationCatalog.Instance.PropertyChanged -= onCatalog;
            LocalizationService.LanguageChanged -= onLanguage;
         }

         _ = LocalizationCatalog.Instance["Not_A_Key"].Should().Be("Not_A_Key");
      }

      [TestMethod]
      public void Case08_ResolveEffectiveLanguageCode_UserOverrideBeatsApp()
      {
         AppLanguage satellite = _satelliteLanguages().First();

         _ = LocalizationService.ResolveEffectiveLanguageCode(LocalizationService.DefaultLanguageCode, satellite.Code)
            .Should().Be(satellite.Code);
         _ = LocalizationService.ResolveEffectiveLanguageCode(satellite.Code, null)
            .Should().Be(satellite.Code);
         _ = LocalizationService.ResolveEffectiveLanguageCode(satellite.Code, string.Empty)
            .Should().Be(satellite.Code);
         _ = LocalizationService.ResolveEffectiveLanguageCode(satellite.Code, "not-a-language")
            .Should().Be(satellite.Code);
         _ = LocalizationService.ResolveEffectiveLanguageCode(LocalizationService.SystemCode, null)
            .Should().Be(LocalizationService.SystemCode);
         _ = LocalizationService.ResolveEffectiveLanguageCode(LocalizationService.DefaultLanguageCode, LocalizationService.SystemCode)
            .Should().Be(LocalizationService.SystemCode);
      }

      [TestMethod]
      public void Case09_GetLanguageOrDefault_FallsBackToSystem()
      {
         _ = LocalizationService.GetLanguageOrDefault(null).Code.Should().Be(LocalizationService.SystemCode);
         _ = LocalizationService.GetLanguageOrDefault(string.Empty).Code.Should().Be(LocalizationService.SystemCode);
         _ = LocalizationService.GetLanguageOrDefault("not-a-language").Code.Should().Be(LocalizationService.SystemCode);
         _ = LocalizationService.GetLanguageOrDefault("FR").Code.Should().Be("fr");
      }

      [TestMethod]
      public void Case10_ResolveCultureCode_SystemFollowsOsSeam()
      {
         AppLanguage satellite = _satelliteLanguages().First();
         LocalizationService.DetectSystemLanguageCode = () => satellite.Code;
         _ = LocalizationService.ResolveCultureCode(LocalizationService.SystemCode).Should().Be(satellite.Code);
         _ = LocalizationService.ResolveDefaultLanguageCode().Should().Be(satellite.Code);

         LocalizationService.DetectSystemLanguageCode = static () => "not-a-language";
         _ = LocalizationService.ResolveCultureCode(LocalizationService.SystemCode)
            .Should().Be(LocalizationService.DefaultLanguageCode);

         _ = LocalizationService.ResolveCultureCode(satellite.Code).Should().Be(satellite.Code);
         _ = LocalizationService.ResolveCultureCode(LocalizationService.DefaultLanguageCode)
            .Should().Be(LocalizationService.DefaultLanguageCode);
      }

      [TestMethod]
      public void Case11_Apply_SystemPreferenceUsesOsSeam()
      {
         AppLanguage satellite = _satelliteLanguages().First();
         LocalizationService.DetectSystemLanguageCode = () => satellite.Code;

         _ = LocalizationService.Apply(LocalizationService.SystemCode).Should().BeTrue();
         _ = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Should().Be(satellite.Code);

         _ = LocalizationService.Apply(LocalizationService.SystemCode).Should().BeFalse();

         _ = LocalizationService.ApplyEffective(LocalizationService.SystemCode, LocalizationService.DefaultLanguageCode)
            .Should().BeTrue();
         _ = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Should().Be(LocalizationService.DefaultLanguageCode);
      }

      [TestMethod]
      public void Case12_NewItemPrefixes_DifferFromEnglish_InEachSatellite()
      {
         string englishService = Strings.Msg_NewServicePrefix;

         foreach (AppLanguage language in _satelliteLanguages())
         {
            _ = LocalizationService.Apply(language.Code);

            _ = Strings.Msg_NewServicePrefix.Should().NotBe(englishService, because: language.Code);
            _ = Strings.EnumValue_FollowApp.Should().NotBe(
               Strings.GetNeutral(nameof(Strings.EnumValue_FollowApp)), because: language.Code);
         }
      }

      [TestMethod]
      public void Case13_EnumDisplayHelper_FormatsFollowAppPreference()
      {
         _ = EnumDisplayHelper.FormatFieldValue("Language", ISettings.FollowAppCode)
            .Should().Be(Strings.EnumValue_FollowApp);
         _ = EnumDisplayHelper.FormatFieldValue("Language", "(app)")
            .Should().Be(Strings.EnumValue_FollowApp);
         _ = EnumDisplayHelper.FormatFieldValue("Theme", ISettings.FollowAppCode)
            .Should().Be(Strings.EnumValue_FollowApp);
         _ = EnumDisplayHelper.FormatFieldValue("Theme", "(app)")
            .Should().Be(Strings.EnumValue_FollowApp);
      }

      [TestMethod]
      public void Case14_EnumDisplayHelper_FormatsLanguageAndThemePreferences()
      {
         _ = EnumDisplayHelper.FormatFieldValue("Language", LocalizationService.SystemCode)
            .Should().Be(Strings.EnumValue_Theme_System);
         _ = EnumDisplayHelper.FormatFieldValue("Language", LocalizationService.DefaultLanguageCode)
            .Should().Be(LocalizationService.DefaultLanguageCode);

         _ = EnumDisplayHelper.FormatFieldValue("Theme", "System")
            .Should().Be(Strings.EnumValue_Theme_System);
         _ = EnumDisplayHelper.FormatFieldValue("Theme", "Light")
            .Should().Be(Strings.EnumValue_Theme_Light);
         _ = EnumDisplayHelper.FormatFieldValue("Theme", "Dark")
            .Should().Be(Strings.EnumValue_Theme_Dark);
      }

      [TestMethod]
      public void Case15_EnumDisplayHelper_FormatsImportExportError()
      {
         _ = EnumDisplayHelper.FormatFieldValue(nameof(ImportExportError), nameof(ImportExportError.None))
            .Should().Be(Strings.EnumValue_ImportExportError_None);
         _ = EnumDisplayHelper.FormatFieldValue(nameof(ImportExportError), nameof(ImportExportError.IncorrectCSVFormat))
            .Should().Be(Strings.EnumValue_ImportExportError_IncorrectCSVFormat);
         _ = EnumDisplayHelper.FormatFieldValue("errorLog", nameof(ImportExportError.ExportFileAlreadyExists))
            .Should().Be(Strings.EnumValue_ImportExportError_ExportFileAlreadyExists);
      }

      [TestMethod]
      /*
       * LoginViewModel labels come from the catalog and follow the culture after
       * OnLanguageChanged, which raises PropertyChanged for the computed labels.
      */
      public void Case16_LoginViewModel_LabelsFollowCulture()
      {
         LoginViewModel viewModel = new();
         viewModel.SelectVault("vault.pku", "vault.pku");

         string englishLabel = viewModel.CredentialsLabel;
         string englishVault = viewModel.SelectedVaultLabel;
         _ = englishLabel.Should().Be(Strings.Label_Username);
         _ = englishVault.Should().Be(Strings.Format(nameof(Strings.Login_SelectedVault), "vault.pku"));
         _ = viewModel.StatusMessage.Should().Be(Strings.Login_EnterVaultUsername);

         List<string?> changed = [];
         viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

         AppLanguage satellite = _satelliteLanguages().First();
         _ = LocalizationService.Apply(satellite.Code);
         viewModel.OnLanguageChanged();

         _ = changed.Should().Contain([nameof(LoginViewModel.CredentialsLabel), nameof(LoginViewModel.SelectedVaultLabel)]);
         _ = viewModel.CredentialsLabel.Should().NotBe(englishLabel);
         _ = viewModel.SelectedVaultLabel.Should().NotBe(englishVault).And.Contain("vault.pku");

         viewModel.Reset();
         _ = viewModel.SelectedVaultLabel.Should().BeEmpty();
      }
   }
}
