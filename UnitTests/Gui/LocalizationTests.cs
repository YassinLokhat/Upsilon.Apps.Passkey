using FluentAssertions;
using System.Globalization;
using System.Resources;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.UnitTests.Gui
{
   [TestClass]
   [DoNotParallelize]
   public sealed class LocalizationTests
   {
      [TestInitialize]
      public void Initialize()
      {
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
      }

      [TestMethod]
      public void FrenchResources_ContainEveryNeutralKey()
      {
         ResourceManager manager = new(
            "Upsilon.Apps.Passkey.GUI.WPF.Localization.Strings",
            typeof(Strings).Assembly);

         using ResourceSet? neutral = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
         using ResourceSet? french = manager.GetResourceSet(CultureInfo.GetCultureInfo("fr"), createIfNotExists: true, tryParents: false);

         _ = neutral.Should().NotBeNull();
         _ = french.Should().NotBeNull();

         foreach (System.Collections.DictionaryEntry entry in neutral!)
         {
            string key = (string)entry.Key;
            _ = french!.GetString(key).Should().NotBeNullOrEmpty($"French resource missing key '{key}'");
         }
      }

      [TestMethod]
      public void NeutralAndFrenchResources_HaveSameKeys()
      {
         ResourceManager manager = new(
            "Upsilon.Apps.Passkey.GUI.WPF.Localization.Strings",
            typeof(Strings).Assembly);

         using ResourceSet? neutral = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
         using ResourceSet? french = manager.GetResourceSet(CultureInfo.GetCultureInfo("fr"), createIfNotExists: true, tryParents: false);

         HashSet<string> neutralKeys = neutral!.Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .ToHashSet(StringComparer.Ordinal);
         HashSet<string> frenchKeys = french!.Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .ToHashSet(StringComparer.Ordinal);

         _ = neutralKeys.Should().BeEquivalentTo(frenchKeys);
      }

      [TestMethod]
      public void SupportedLanguages_IncludeEnglishAndFrench()
      {
         _ = LocalizationService.Supported.Select(l => l.Code)
            .Should().Contain(["en", "fr"]);
      }

      [TestMethod]
      public void EnumDisplayHelper_FormatsAccountOptionFlags_InEnglish()
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
      public void EnumDisplayHelper_FormatsWarningTypeFlags_InFrench()
      {
         LocalizationService.Apply("fr");

         _ = EnumDisplayHelper.FormatFieldValue("WarningsToNotify", "None")
            .Should().Be(Strings.EnumValue_None);

         string combined = EnumDisplayHelper.FormatFieldValue("WarningsToNotify",
            $"{nameof(WarningType.ActivityReviewWarning)}, {nameof(WarningType.PasswordLeakedWarning)}");

         _ = combined.Should().Be($"{Strings.Label_NotifyActivityReview}, {Strings.Label_NotifyPasswordLeaked}");
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
      public void ActivityEventType_ToReadableString_IsLocalizedInFrench()
      {
         LocalizationService.Apply("en");
         string englishLabel = ActivityEventType.DatabaseOpened.ToReadableString();

         LocalizationService.Apply("fr");
         string frenchLabel = ActivityEventType.DatabaseOpened.ToReadableString();

         _ = frenchLabel.Should().NotBe(nameof(ActivityEventType.DatabaseOpened));
         _ = frenchLabel.Should().NotBe(englishLabel);
      }

      [TestMethod]
      public void Apply_RaisesLanguageChanged_OnlyWhenCultureChanges()
      {
         LocalizationService.Apply("en");

         int raised = 0;
         void handler(object? _, EventArgs __) => raised++;
         LocalizationService.LanguageChanged += handler;
         try
         {
            _ = LocalizationService.Apply("en").Should().BeFalse();
            _ = raised.Should().Be(0);

            _ = LocalizationService.Apply("fr").Should().BeTrue();
            _ = raised.Should().Be(1);

            _ = LocalizationService.Apply("fr").Should().BeFalse();
            _ = raised.Should().Be(1);
         }
         finally
         {
            LocalizationService.LanguageChanged -= handler;
         }
      }

      [TestMethod]
      public void TranslationSource_Indexer_FollowsCurrentUiCulture()
      {
         LocalizationService.Apply("en");
         string english = TranslationSource.Instance[nameof(Strings.Menu_Save)];

         LocalizationService.Apply("fr");
         string french = TranslationSource.Instance[nameof(Strings.Menu_Save)];

         _ = french.Should().NotBe(english);
         _ = french.Should().Be(Strings.Menu_Save);
      }

      [TestCleanup]
      public void Cleanup()
      {
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
      }
   }
}
