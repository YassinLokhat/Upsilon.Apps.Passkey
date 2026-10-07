using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Multiplateform.Core
{
   [TestClass]
   public sealed class ServiceUrlHelperUnitTests
   {
      [TestMethod]
      public void TryCreateAllowedUri_Empty_SucceedsWithNull()
      {
         ServiceUrlHelper.TryCreateAllowedUri("  ", out Uri? uri).Should().BeTrue();
         uri.Should().BeNull();
      }

      [TestMethod]
      public void TryCreateAllowedUri_Https_Succeeds()
      {
         ServiceUrlHelper.TryCreateAllowedUri("https://example.com/path", out Uri? uri).Should().BeTrue();
         uri.Should().NotBeNull();
         uri!.Scheme.Should().Be(Uri.UriSchemeHttps);
      }

      [TestMethod]
      public void TryCreateAllowedUri_Http_Succeeds()
      {
         ServiceUrlHelper.TryCreateAllowedUri("http://example.com", out Uri? uri).Should().BeTrue();
         uri.Should().NotBeNull();
         uri!.Scheme.Should().Be(Uri.UriSchemeHttp);
      }

      [TestMethod]
      [DataRow("file:///C:/secrets.txt")]
      [DataRow("javascript:alert(1)")]
      [DataRow("\\\\server\\share")]
      [DataRow("/relative/path")]
      [DataRow("not a uri")]
      [DataRow("ftp://example.com")]
      public void TryCreateAllowedUri_Disallowed_Fails(string value)
      {
         ServiceUrlHelper.TryCreateAllowedUri(value, out Uri? uri).Should().BeFalse();
         uri.Should().BeNull();
      }

      [TestMethod]
      public void ClassifyForOpen_Https_OpenDirect()
      {
         ServiceUrlHelper.ClassifyForOpen("https://example.com")
            .Should().Be(ServiceUrlHelper.OpenDisposition.OpenDirect);
      }

      [TestMethod]
      public void ClassifyForOpen_Http_RequiresConfirmation()
      {
         ServiceUrlHelper.ClassifyForOpen("http://example.com")
            .Should().Be(ServiceUrlHelper.OpenDisposition.RequiresHttpConfirmation);
      }

      [TestMethod]
      public void ClassifyForOpen_File_Rejected()
      {
         ServiceUrlHelper.ClassifyForOpen("file:///C:/x")
            .Should().Be(ServiceUrlHelper.OpenDisposition.Rejected);
      }

      [TestMethod]
      public void ClassifyForOpen_Empty_Rejected()
      {
         ServiceUrlHelper.ClassifyForOpen(null)
            .Should().Be(ServiceUrlHelper.OpenDisposition.Rejected);
         ServiceUrlHelper.ClassifyForOpen("")
            .Should().Be(ServiceUrlHelper.OpenDisposition.Rejected);
      }
   }
}
