using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;

namespace Upsilon.Apps.Passkey.UnitTests.Gui
{
   [TestClass]
   [DoNotParallelize]
   public sealed class ThemeServiceTests
   {
      [TestInitialize]
      public void Initialize()
      {
         ThemeService.DetectSystemLightTheme = static () => false;
         _ = ThemeService.Apply(ThemeService.DarkCode);
      }

      [TestCleanup]
      public void Cleanup()
      {
         ThemeService.DetectSystemLightTheme = static () => false;
         _ = ThemeService.Apply(ThemeService.DarkCode);
         ThemeService.Shutdown();
      }

      [TestMethod]
      public void GetThemeOrDefault_FallsBackToSystem()
      {
         _ = ThemeService.GetThemeOrDefault(null).Should().Be(ThemeService.SystemCode);
         _ = ThemeService.GetThemeOrDefault(string.Empty).Should().Be(ThemeService.SystemCode);
         _ = ThemeService.GetThemeOrDefault("not-a-theme").Should().Be(ThemeService.SystemCode);
         _ = ThemeService.GetThemeOrDefault("light").Should().Be(ThemeService.LightCode);
      }

      [TestMethod]
      public void ResolveEffectivePreference_UserOverrideBeatsApp()
      {
         _ = ThemeService.ResolveEffectivePreference(ThemeService.DarkCode, ThemeService.LightCode)
            .Should().Be(ThemeService.LightCode);

         _ = ThemeService.ResolveEffectivePreference(ThemeService.DarkCode, null)
            .Should().Be(ThemeService.DarkCode);

         _ = ThemeService.ResolveEffectivePreference(ThemeService.DarkCode, string.Empty)
            .Should().Be(ThemeService.DarkCode);

         _ = ThemeService.ResolveEffectivePreference(ThemeService.DarkCode, "not-a-theme")
            .Should().Be(ThemeService.DarkCode);
      }

      [TestMethod]
      public void ResolveAppearance_SystemFollowsOsSeam()
      {
         ThemeService.DetectSystemLightTheme = static () => true;
         _ = ThemeService.ResolveAppearance(ThemeService.SystemCode).Should().Be(ThemeService.LightCode);

         ThemeService.DetectSystemLightTheme = static () => false;
         _ = ThemeService.ResolveAppearance(ThemeService.SystemCode).Should().Be(ThemeService.DarkCode);

         _ = ThemeService.ResolveAppearance(ThemeService.LightCode).Should().Be(ThemeService.LightCode);
         _ = ThemeService.ResolveAppearance(ThemeService.DarkCode).Should().Be(ThemeService.DarkCode);
      }

      [TestMethod]
      public void ApplyEffective_UserOverrideBeatsApp()
      {
         _ = ThemeService.ApplyEffective(ThemeService.DarkCode, ThemeService.LightCode).Should().BeTrue();
         _ = ThemeService.CurrentPreference.Should().Be(ThemeService.LightCode);
         _ = ThemeService.CurrentAppearance.Should().Be(ThemeService.LightCode);
         _ = ThemeService.IsDarkAppearance.Should().BeFalse();

         _ = ThemeService.ApplyEffective(ThemeService.DarkCode, ThemeService.LightCode).Should().BeFalse();
      }

      [TestMethod]
      public void Apply_SystemPreferenceUsesOsSeam()
      {
         ThemeService.DetectSystemLightTheme = static () => true;
         _ = ThemeService.Apply(ThemeService.SystemCode).Should().BeTrue();
         _ = ThemeService.CurrentPreference.Should().Be(ThemeService.SystemCode);
         _ = ThemeService.CurrentAppearance.Should().Be(ThemeService.LightCode);
      }
   }
}
