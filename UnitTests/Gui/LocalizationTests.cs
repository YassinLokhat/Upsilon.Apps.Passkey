using FluentAssertions;
using System.Globalization;
using System.Resources;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.UnitTests;

namespace Upsilon.Apps.Passkey.UnitTests.Gui
{
   [TestClass]
   [DoNotParallelize]
   public sealed class LocalizationTests
   {
      private static readonly ResourceManager Manager = new(
         "Upsilon.Apps.Passkey.GUI.WPF.Localization.Strings",
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
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
      }

      [TestMethod]
      public void SupportedLanguages_IncludeSystemDefaultAndAtLeastOneSatellite()
      {
         IReadOnlyList<string> codes = [.. LocalizationService.Supported.Select(l => l.Code)];

         _ = codes.Should().Contain(LocalizationService.SystemCode);
         _ = codes.Should().Contain(LocalizationService.DefaultLanguageCode);
         _ = codes[0].Should().Be(LocalizationService.SystemCode);
         _ = codes.Should().OnlyHaveUniqueItems();
         _ = LocalizationService.Shipped.Select(l => l.Code).Should().NotContain(LocalizationService.SystemCode);
         _ = _satelliteLanguages().Should().NotBeEmpty(
            "at least one translated satellite (e.g. fr) must ship with the client");
      }

      [TestMethod]
      public void SatelliteResources_ContainEveryNeutralKey()
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
      public void EnumDisplayHelper_FormatsAccountOptionFlags_InDefaultLanguage()
      {
         _ = EnumDisplayHelper.FormatFieldValue("Options", "None")
            .Should().Be(Strings.EnumValue_None);

         _ = EnumDisplayHelper.FormatFieldValue("Options", nameof(AccountOption.WarnIfPasswordLeaked))
            .Should().Be(Strings.Label_WarnPasswordLeak);

         string combined = EnumDisplayHelper.FormatFieldValue("Options",
            $"{nameof(AccountOption.WarnIfPasswordLeaked)}, {nameof(AccountOption.WarnIfDuplicatedPassword)}");

         _ = combined.Should().Be($"{Strings.Label_WarnPasswordLeak}, {Strings.Label_WarnDuplicatedPassword}");
      }

      [TestMethod]
      public void EnumDisplayHelper_FormatsWarningTypeFlags_InEachSatelliteLanguage()
      {
         foreach (AppLanguage language in _satelliteLanguages())
         {
            LocalizationService.Apply(language.Code);

            _ = EnumDisplayHelper.FormatFieldValue("WarningsToNotify", "None")
               .Should().Be(Strings.EnumValue_None, because: language.Code);

            string combined = EnumDisplayHelper.FormatFieldValue("WarningsToNotify",
               $"{nameof(WarningType.ActivityReviewWarning)}, {nameof(WarningType.PasswordLeakedWarning)}");

            _ = combined.Should().Be(
               $"{Strings.Label_NotifyActivityReview}, {Strings.Label_NotifyPasswordLeaked}",
               because: language.Code);
         }
      }

      [TestMethod]
      public void ActivityEventType_ToReadableString_HasTranslationForEveryMember()
      {
         foreach (ActivityEventType eventType in Enum.GetValues<ActivityEventType>())
         {
            if (eventType == ActivityEventType.None)
            {
               _ = eventType.ToReadableString().Should().Be(Strings.Filter_All);
               continue;
            }

            string label = eventType.ToReadableString();
            _ = label.Should().NotBeNullOrWhiteSpace();
            _ = label.Should().NotBe(eventType.ToString());
         }
      }

      [TestMethod]
      public void WarningType_ToReadableString_HasTranslationForEveryMember()
      {
         foreach (WarningType warningType in Enum.GetValues<WarningType>())
         {
            string label = warningType.ToReadableString();
            _ = label.Should().NotBeNullOrWhiteSpace();
            _ = label.Should().NotBe(warningType.ToString());
         }

         _ = (WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning)
            .ToReadableString()
            .Should().Be(Strings.Filter_All);
      }

      [TestMethod]
      public void ActivityEventType_ToReadableString_DiffersFromEnglish_InEachSatellite()
      {
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
         string englishLabel = ActivityEventType.DatabaseOpened.ToReadableString();

         foreach (AppLanguage language in _satelliteLanguages())
         {
            LocalizationService.Apply(language.Code);
            string localized = ActivityEventType.DatabaseOpened.ToReadableString();

            _ = localized.Should().NotBe(nameof(ActivityEventType.DatabaseOpened), because: language.Code);
            _ = localized.Should().NotBe(englishLabel, because: language.Code);
         }
      }

      [TestMethod]
      public void Apply_RaisesLanguageChanged_OnlyWhenCultureChanges()
      {
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode);

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
         }
         finally
         {
            LocalizationService.LanguageChanged -= handler;
         }
      }

      [TestMethod]
      public void TranslationSource_Indexer_FollowsEachSatelliteCulture()
      {
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
         string english = TranslationSource.Instance[nameof(Strings.Menu_Save)];

         foreach (AppLanguage language in _satelliteLanguages())
         {
            LocalizationService.Apply(language.Code);
            string localized = TranslationSource.Instance[nameof(Strings.Menu_Save)];

            _ = localized.Should().NotBe(english, because: language.Code);
            _ = localized.Should().Be(Strings.Menu_Save, because: language.Code);
         }
      }

      [TestMethod]
      public void ResolveEffectiveLanguageCode_UserOverrideBeatsApp()
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
      public void GetLanguageOrDefault_FallsBackToSystem()
      {
         _ = LocalizationService.GetLanguageOrDefault(null).Code.Should().Be(LocalizationService.SystemCode);
         _ = LocalizationService.GetLanguageOrDefault(string.Empty).Code.Should().Be(LocalizationService.SystemCode);
         _ = LocalizationService.GetLanguageOrDefault("not-a-language").Code.Should().Be(LocalizationService.SystemCode);
         _ = LocalizationService.GetLanguageOrDefault("FR").Code.Should().Be("fr");
      }

      [TestMethod]
      public void ResolveCultureCode_SystemFollowsOsSeam()
      {
         AppLanguage satellite = _satelliteLanguages().First();
         LocalizationService.DetectSystemLanguageCode = () => satellite.Code;
         _ = LocalizationService.ResolveCultureCode(LocalizationService.SystemCode).Should().Be(satellite.Code);

         LocalizationService.DetectSystemLanguageCode = static () => "not-a-language";
         _ = LocalizationService.ResolveCultureCode(LocalizationService.SystemCode)
            .Should().Be(LocalizationService.DefaultLanguageCode);

         _ = LocalizationService.ResolveCultureCode(satellite.Code).Should().Be(satellite.Code);
         _ = LocalizationService.ResolveCultureCode(LocalizationService.DefaultLanguageCode)
            .Should().Be(LocalizationService.DefaultLanguageCode);
      }

      [TestMethod]
      public void Apply_SystemPreferenceUsesOsSeam()
      {
         AppLanguage satellite = _satelliteLanguages().First();
         LocalizationService.DetectSystemLanguageCode = () => satellite.Code;

         _ = LocalizationService.Apply(LocalizationService.SystemCode).Should().BeTrue();
         _ = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Should().Be(satellite.Code);

         _ = LocalizationService.Apply(LocalizationService.SystemCode).Should().BeFalse();
      }

      [TestMethod]
      public void EnumDisplayHelper_FormatsLanguagePreference()
      {
         _ = EnumDisplayHelper.FormatFieldValue("Language", LocalizationService.SystemCode)
            .Should().Be(Strings.EnumValue_Theme_System);
         _ = EnumDisplayHelper.FormatFieldValue("Language", LocalizationService.DefaultLanguageCode)
            .Should().Be(LocalizationService.DefaultLanguageCode);
      }

      [TestMethod]
      public void EnumDisplayHelper_FormatsThemePreference()
      {
         _ = EnumDisplayHelper.FormatFieldValue("Theme", "System")
            .Should().Be(Strings.EnumValue_Theme_System);
         _ = EnumDisplayHelper.FormatFieldValue("Theme", "Light")
            .Should().Be(Strings.EnumValue_Theme_Light);
         _ = EnumDisplayHelper.FormatFieldValue("Theme", "Dark")
            .Should().Be(Strings.EnumValue_Theme_Dark);
      }

      [TestMethod]
      public void EnumDisplayHelper_FormatsImportExportError()
      {
         _ = EnumDisplayHelper.FormatFieldValue(nameof(ImportExportError), nameof(ImportExportError.None))
            .Should().Be(Strings.EnumValue_ImportExportError_None);
         _ = EnumDisplayHelper.FormatFieldValue(nameof(ImportExportError), nameof(ImportExportError.IncorrectCSVFormat))
            .Should().Be(Strings.EnumValue_ImportExportError_IncorrectCSVFormat);
         _ = EnumDisplayHelper.FormatFieldValue("errorLog", nameof(ImportExportError.ExportFileAlreadyExists))
            .Should().Be(Strings.EnumValue_ImportExportError_ExportFileAlreadyExists);
      }

      [TestMethod]
      public void ImportExportFailureMessages_UseLocalizedEnumLabels()
      {
         foreach (ImportExportError error in Enum.GetValues<ImportExportError>())
         {
            if (error == ImportExportError.None)
            {
               continue;
            }

            ActivityViewModel importFailed = new(new Activity(
               DateTime.Now.Ticks,
               "id",
               username: null,
               serviceName: null,
               accountName: null,
               fieldName: nameof(ImportExportError),
               fieldValue: error.ToString(),
               parentName: null,
               ActivityEventType.ImportingDataFailed,
               needsReview: true));

            _ = importFailed.Message.Should().Be(
               UnitTestsHelper.FormatImportFailed(error).Split(" : ", 2)[1],
               because: error.ToString());

            ActivityViewModel exportFailed = new(new Activity(
               DateTime.Now.Ticks,
               "id",
               username: null,
               serviceName: null,
               accountName: null,
               fieldName: nameof(ImportExportError),
               fieldValue: error.ToString(),
               parentName: null,
               ActivityEventType.ExportingDataFailed,
               needsReview: true));

            _ = exportFailed.Message.Should().Be(
               UnitTestsHelper.FormatExportFailed(error).Split(" : ", 2)[1],
               because: error.ToString());

            _ = importFailed.Message.Should().NotBe(error.ToString(), because: error.ToString());
         }
      }

      [TestCleanup]
      public void Cleanup()
      {
         LocalizationService.DetectSystemLanguageCode = static () => LocalizationService.DefaultLanguageCode;
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
      }
   }
}
