using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Activities;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests.Windows.Helpers;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// WPF-only localization surfaces (enum helpers, activity rows). The shared
   /// catalog, language resolution and resource parity are covered in
   /// <c>UnitTests.Presentation.LocalizationTests</c>.
   /// </summary>
   [TestClass]
   [DoNotParallelize]
   public sealed class LocalizationTests
   {
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
      public void AlertKinds_ToReadableAlertKind_HasTranslationForEveryKnownKind()
      {
         foreach (string kind in AlertKinds.DefaultNotify)
         {
            string label = EnumHelper.ToReadableAlertKind(kind);
            _ = label.Should().NotBeNullOrWhiteSpace();
            _ = label.Should().NotBe(kind);
         }

         _ = EnumHelper.ToReadableAlertKind(EnumHelper.AccountPasswordFilterAll)
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
               ActivityAssertHelper.FormatImportFailed(error).Split(" : ", 2)[1],
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
               ActivityAssertHelper.FormatExportFailed(error).Split(" : ", 2)[1],
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
