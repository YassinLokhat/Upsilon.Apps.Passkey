using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Controls;

namespace UnitTests.Presentation
{
   [TestClass]
   public sealed class SecretFieldTests
   {
      [TestMethod]
      /*
       * A mirroring field (WPF) copies the secret only while revealed, follows
       * edits made while revealed, and drops the copy when masked again.
      */
      public void Case01_Mirroring_CopiesOnlyWhileRevealed()
      {
         CountingSecretInput input = new() { Value = "first" };
         SecretField field = new(input, mirrorsPlaintext: true);

         field.RefreshMirror();
         _ = field.RevealText.Should().BeEmpty();
         _ = input.Reads.Should().Be(0);

         field.Reveal();
         _ = field.IsRevealed.Should().BeTrue();
         _ = field.RevealText.Should().Be("first");

         input.Value = "second";
         field.RefreshMirror();
         _ = field.RevealText.Should().Be("second");

         field.Mask();
         _ = field.IsRevealed.Should().BeFalse();
         _ = field.RevealText.Should().BeEmpty();

         input.Value = "third";
         field.RefreshMirror();
         _ = field.RevealText.Should().BeEmpty();
         _ = input.Reads.Should().Be(2);
      }

      [TestMethod]
      /*
       * A non-mirroring field (MAUI unmasks the Entry itself) never reads the
       * secret, even while revealed.
      */
      public void Case02_NonMirroring_NeverReadsTheSecret()
      {
         CountingSecretInput input = new() { Value = "secret" };
         SecretField field = new(input, mirrorsPlaintext: false);
         List<string?> changed = [];
         field.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

         field.Reveal();
         field.RefreshMirror();

         _ = field.IsRevealed.Should().BeTrue();
         _ = field.RevealText.Should().BeEmpty();
         _ = input.Reads.Should().Be(0);
         _ = changed.Should().Contain(nameof(SecretField.IsRevealed));
      }

      [TestMethod]
      /*
       * Clear masks the field and empties the host control.
      */
      public void Case03_Clear_MasksThenEmptiesTheControl()
      {
         CountingSecretInput input = new() { Value = "secret" };
         SecretField field = new(input, mirrorsPlaintext: true);
         field.Reveal();

         field.Clear();

         _ = field.IsRevealed.Should().BeFalse();
         _ = field.RevealText.Should().BeEmpty();
         _ = input.Value.Should().BeEmpty();
      }

      private sealed class CountingSecretInput : ISecretInput
      {
         public string Value { get; set; } = string.Empty;

         public int Reads { get; private set; }

         public string Read()
         {
            Reads++;
            return Value;
         }

         public void Write(string secret) => Value = secret;

         public void Clear() => Value = string.Empty;
      }
   }
}
