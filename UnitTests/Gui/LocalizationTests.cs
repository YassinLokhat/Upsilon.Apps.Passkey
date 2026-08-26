using FluentAssertions;
using System.Globalization;
using System.Resources;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.UnitTests.Gui
{
   [TestClass]
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
      public void SupportedLanguages_IncludeEnglishAndFrench()
      {
         _ = LocalizationService.Supported.Select(l => l.Code)
            .Should().Contain(["en", "fr"]);
      }

      [TestMethod]
      public void EnumDisplayHelper_FormatsAccountOptionFlags_InEnglish()
      {
         _ = EnumDisplayHelper.FormatFieldValue("Options", "None")
            .Should().Be("None");

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
            .Should().Be("Aucune");

         string combined = EnumDisplayHelper.FormatFieldValue("WarningsToNotify",
            $"{nameof(WarningType.ActivityReviewWarning)}, {nameof(WarningType.PasswordLeakedWarning)}");

         _ = combined.Should().Be($"{Strings.Label_NotifyActivityReview}, {Strings.Label_NotifyPasswordLeaked}");
      }

      [TestCleanup]
      public void Cleanup()
      {
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
      }
   }
}
