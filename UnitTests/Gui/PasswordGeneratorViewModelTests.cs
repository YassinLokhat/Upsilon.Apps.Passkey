using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.UnitTests.Gui
{
   [TestClass]
   [DoNotParallelize]
   public sealed class PasswordGeneratorViewModelTests
   {
      [TestInitialize]
      public void Initialize() => GuiTestServices.Install();

      [TestCleanup]
      public void Cleanup() => GuiTestServices.Reset();

      [TestMethod]
      public void Case01_Alphabet_IncludesSelectedCharacterClasses()
      {
         PasswordGeneratorViewModel vm = new();

         vm.CheckIfLeaked = false;
         vm.IncludeNumerics = true;
         vm.IncludeUpperCaseAlphabet = true;
         vm.IncludeLowerCaseAlphabet = true;
         vm.IncludeSpecialCharacters = true;

         string alphabet = vm.Alphabet;
         var factory = AppServices.PasswordFactory;

         _ = alphabet.Should().Contain(factory.Numeric);
         _ = alphabet.Should().Contain(factory.Alphabetic.ToUpperInvariant());
         _ = alphabet.Should().Contain(factory.Alphabetic.ToLowerInvariant());
         _ = alphabet.Should().Contain(factory.SpecialChars);
      }

      [TestMethod]
      public void Case02_Alphabet_ExcludesDisabledCharacterClasses()
      {
         PasswordGeneratorViewModel vm = new();

         vm.CheckIfLeaked = false;
         vm.IncludeNumerics = false;
         vm.IncludeUpperCaseAlphabet = false;
         vm.IncludeLowerCaseAlphabet = true;
         vm.IncludeSpecialCharacters = false;

         string expected = AppServices.PasswordFactory.Alphabetic.ToLowerInvariant();

         _ = vm.Alphabet.Should().Be(expected);
      }

      [TestMethod]
      public async Task Case03_GeneratePassword_RespectsLengthWithoutLeakCheck()
      {
         PasswordGeneratorViewModel vm = new();

         vm.CheckIfLeaked = false;
         vm.PasswordLength = 24;

         string password = await WaitForPasswordAsync(vm, expectedLength: 24).ConfigureAwait(false);

         _ = password.Should().HaveLength(24);
         _ = password.All(c => vm.Alphabet.Contains(c)).Should().BeTrue();
      }

      [TestMethod]
      public async Task Case04_CopyCommand_WritesGeneratedPasswordToClipboard()
      {
         PasswordGeneratorViewModel vm = new();

         vm.CheckIfLeaked = false;
         vm.GeneratePassword();
         string password = await WaitForPasswordAsync(vm, expectedLength: vm.PasswordLength).ConfigureAwait(false);

         GuiTestServices.Clipboard.Clear();
         vm.CopyCommand.Execute(null);

         _ = GuiTestServices.Clipboard.LastText.Should().Be(password);
         _ = GuiTestServices.Clipboard.Texts.Should().ContainSingle().Which.Should().Be(password);
      }

      private static async Task<string> WaitForPasswordAsync(PasswordGeneratorViewModel vm, int expectedLength, int timeoutMs = 5000)
      {
         DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

         while (vm.GeneratedPassword.Length != expectedLength)
         {
            if (DateTime.UtcNow >= deadline)
            {
               throw new TimeoutException(
                  $"Password generation did not complete in time (got length {vm.GeneratedPassword.Length}, expected {expectedLength}).");
            }

            await Task.Delay(20).ConfigureAwait(false);
         }

         return vm.GeneratedPassword;
      }
   }
}
