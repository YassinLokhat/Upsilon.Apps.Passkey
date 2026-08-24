using FluentAssertions;
using System.Globalization;
using System.Resources;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;

namespace Upsilon.Apps.Passkey.UnitTests.Gui
{
   [TestClass]
   public sealed class LocalizationTests
   {
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

      [TestCleanup]
      public void Cleanup()
      {
         LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
      }
   }
}
