using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;

namespace UnitTests.Presentation
{
   [TestClass]
   public sealed class FieldVisualStateTests
   {
      [TestMethod]
      public void FromFlags_MapsDirtyWeakAndLeak()
      {
         FieldVisualStateHelper.FromFlags(isDirty: false, isNotifiedWeak: false, isNotifiedLeak: false)
            .Should().Be(FieldVisualState.Default);
         FieldVisualStateHelper.FromFlags(isDirty: true, isNotifiedWeak: false, isNotifiedLeak: false)
            .Should().Be(FieldVisualState.Dirty);
         FieldVisualStateHelper.FromFlags(isDirty: false, isNotifiedWeak: true, isNotifiedLeak: false)
            .Should().Be(FieldVisualState.Weak);
         FieldVisualStateHelper.FromFlags(isDirty: false, isNotifiedWeak: false, isNotifiedLeak: true)
            .Should().Be(FieldVisualState.Leaked);
         FieldVisualStateHelper.FromFlags(isDirty: true, isNotifiedWeak: true, isNotifiedLeak: true)
            .Should().Be(FieldVisualState.WeakAndLeaked);
      }
   }
}
