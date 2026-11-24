using System.Windows.Controls;
using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.WPF.Adapters;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   [TestClass]
   public sealed class PasswordBoxSecretInputTests
   {
      [STATestMethod]
      /*
       * The adapter reads the PasswordBox through its SecureString, writes into
       * it and clears it; an empty box reads as an empty string.
      */
      public void ReadWriteClear_GoThroughThePasswordBox()
      {
         PasswordBox box = new();
         PasswordBoxSecretInput input = new(box);
         _ = input.Read().Should().BeEmpty();

         input.Write("pässkey 1");
         _ = box.Password.Should().Be("pässkey 1");
         _ = input.Read().Should().Be("pässkey 1");

         box.Password = "typed";
         _ = input.Read().Should().Be("typed");

         input.Clear();
         _ = box.Password.Should().BeEmpty();
         _ = input.Read().Should().BeEmpty();
      }
   }
}
