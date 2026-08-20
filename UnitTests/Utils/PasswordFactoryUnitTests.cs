using FluentAssertions;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Core.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   [TestClass]
   public sealed class PasswordFactoryUnitTests
   {
      [TestMethod]
      /*
       * The three built-in alphabets should be non-empty and mutually distinct
       * so that a caller can freely combine them.
      */
      public void Case01_BuiltInAlphabets()
      {
         // Given / When
         string alphabetic = UnitTestsHelper.PasswordFactory.Alphabetic;
         string numeric = UnitTestsHelper.PasswordFactory.Numeric;
         string specialChars = UnitTestsHelper.PasswordFactory.SpecialChars;

         // Then
         _ = alphabetic.Should().NotBeNullOrEmpty();
         _ = numeric.Should().Be("0123456789");
         _ = specialChars.Should().NotBeNullOrEmpty();

         _ = numeric.Any(alphabetic.Contains).Should().BeFalse();
      }

      [TestMethod]
      /*
       * A generated password should have the exact requested length.
      */
      public void Case02_GeneratePassword_RespectsLength()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            int length = UnitTestsHelper.GetRandomInt(1, 64);
            string alphabet = UnitTestsHelper.PasswordFactory.Alphabetic
               + UnitTestsHelper.PasswordFactory.Numeric
               + UnitTestsHelper.PasswordFactory.SpecialChars;

            // When
            string password = UnitTestsHelper.PasswordFactory.GeneratePassword(length, alphabet, checkIfLeaked: false);

            // Then
            _ = password.Length.Should().Be(length);
         }
      }

      [TestMethod]
      /*
       * A generated password should only contain characters from the given alphabet.
      */
      public void Case03_GeneratePassword_OnlyUsesAlphabet()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string alphabet = UnitTestsHelper.PasswordFactory.Numeric;

            // When
            string password = UnitTestsHelper.PasswordFactory.GeneratePassword(32, alphabet, checkIfLeaked: false);

            // Then
            _ = password.ToCharArray().Should().OnlyContain(c => alphabet.Contains(c));
         }
      }

      [TestMethod]
      /*
       * Two consecutive generations of a long password should differ, proving the
       * generator draws fresh randomness each time.
      */
      public void Case04_GeneratePassword_IsRandom()
      {
         // Given
         string alphabet = UnitTestsHelper.PasswordFactory.Alphabetic
            + UnitTestsHelper.PasswordFactory.Numeric
            + UnitTestsHelper.PasswordFactory.SpecialChars;

         // When
         string first = UnitTestsHelper.PasswordFactory.GeneratePassword(64, alphabet, checkIfLeaked: false);
         string second = UnitTestsHelper.PasswordFactory.GeneratePassword(64, alphabet, checkIfLeaked: false);

         // Then
         _ = first.Should().NotBe(second);
      }

      [TestMethod]
      /*
       * A non-positive length yields an empty password.
      */
      public void Case05_GeneratePassword_NonPositiveLength()
      {
         // Given
         string alphabet = UnitTestsHelper.PasswordFactory.Alphabetic;

         // When
         string zeroLength = UnitTestsHelper.PasswordFactory.GeneratePassword(0, alphabet, checkIfLeaked: false);
         string negativeLength = UnitTestsHelper.PasswordFactory.GeneratePassword(-5, alphabet, checkIfLeaked: false);

         // Then
         _ = zeroLength.Should().BeEmpty();
         _ = negativeLength.Should().BeEmpty();
      }

      [TestMethod]
      /*
       * A blank alphabet yields an empty password.
      */
      public void Case06_GeneratePassword_BlankAlphabet()
      {
         // Given / When
         string emptyAlphabet = UnitTestsHelper.PasswordFactory.GeneratePassword(10, string.Empty, checkIfLeaked: false);
         string whitespaceAlphabet = UnitTestsHelper.PasswordFactory.GeneratePassword(10, "   ", checkIfLeaked: false);

         // Then
         _ = emptyAlphabet.Should().BeEmpty();
         _ = whitespaceAlphabet.Should().BeEmpty();
      }

      [TestMethod]
      /*
       * The asynchronous generation honours the same contract as the synchronous
       * one: requested length, alphabet, and the empty-password guards.
      */
      public async Task Case07_GeneratePasswordAsync_MatchesSynchronousContract()
      {
         // Given
         string alphabet = UnitTestsHelper.PasswordFactory.Numeric;

         // When
         string password = await UnitTestsHelper.PasswordFactory.GeneratePasswordAsync(32, alphabet, checkIfLeaked: false);
         string zeroLength = await UnitTestsHelper.PasswordFactory.GeneratePasswordAsync(0, alphabet, checkIfLeaked: false);
         string blankAlphabet = await UnitTestsHelper.PasswordFactory.GeneratePasswordAsync(10, "   ", checkIfLeaked: false);

         // Then
         _ = password.Length.Should().Be(32);
         _ = password.ToCharArray().Should().OnlyContain(c => alphabet.Contains(c));
         _ = zeroLength.Should().BeEmpty();
         _ = blankAlphabet.Should().BeEmpty();
      }

      [TestMethod]
      /*
       * A cancelled leak check surfaces the cancellation instead of silently
       * reporting "not leaked", which is how a caller tells a deliberate abort
       * apart from an unreachable service.
      */
      public async Task Case08_PasswordLeakedAsync_HonoursCancellation()
      {
         // Given
         using CancellationTokenSource cancellation = new();
         await cancellation.CancelAsync();

         // When
         Func<Task> act = async () => await UnitTestsHelper.PasswordFactory
            .PasswordLeakedAsync(UnitTestsHelper.GetRandomString(), cancellation.Token);

         // Then
         _ = await act.Should().ThrowAsync<OperationCanceledException>();
      }

      [TestMethod]
      /*
       * A successful HIBP range is cached by prefix: checking the same password
       * twice must hit the network only once.
      */
      public void Case09_PasswordLeaked_CachesRangeByPrefix()
      {
         // Given
         const string password = "unique-test-password-for-cache";
         string hash = _sha1Hex(password);
         RoutingHandler handler = new(
            hibp: _ => (HttpStatusCode.OK, $"{hash[5..]}:1\r\n"),
            xon: _ => (HttpStatusCode.OK, "{\"SearchPassAnon\":{}}"));
         PasswordFactory factory = _factoryFor(handler);

         // When
         bool first = factory.PasswordLeaked(password);
         bool second = factory.PasswordLeaked(password);

         // Then
         _ = first.Should().BeTrue();
         _ = second.Should().BeTrue();
         _ = handler.HibpRequestCount.Should().Be(1);
         _ = handler.XonRequestCount.Should().Be(0);
         _ = factory.CachedRangeCount.Should().Be(1);
      }

      [TestMethod]
      /*
       * A non-success HTTP response must not be cached, otherwise a transient
       * outage would pin "not leaked" for the rest of the process. When HIBP
       * and XON both fail, each PasswordLeaked call asks both providers once.
      */
      public void Case10_PasswordLeaked_DoesNotCacheHttpFailures()
      {
         // Given
         RoutingHandler handler = new(
            hibp: _ => (HttpStatusCode.ServiceUnavailable, null),
            xon: _ => (HttpStatusCode.ServiceUnavailable, null));
         PasswordFactory factory = _factoryFor(handler);

         // When
         bool first = factory.PasswordLeaked("any-password");
         bool second = factory.PasswordLeaked("any-password");

         // Then
         _ = first.Should().BeFalse();
         _ = second.Should().BeFalse();
         _ = handler.HibpRequestCount.Should().Be(2);
         _ = handler.XonRequestCount.Should().Be(2);
         _ = factory.CachedRangeCount.Should().Be(0);
         _ = factory.CachedXonPrefixCount.Should().Be(0);
      }

      [TestMethod]
      /*
       * When every candidate is reported leaked, generation stops after the
       * fixed attempt budget and returns empty - and the shared prefix cache
       * means those retries still cost a single network round-trip.
      */
      public void Case11_GeneratePassword_GivesUpAfterLeakedAttempts()
      {
         // Given: a one-character alphabet yields a single possible password,
         // whose HIBP range we mark as leaked.
         const string alphabet = "A";
         string hash = _sha1Hex("A");
         RoutingHandler handler = new(
            hibp: _ => (HttpStatusCode.OK, $"{hash[5..]}:99\r\n"),
            xon: _ => (HttpStatusCode.NotFound, "{\"Error\":\"Not found\"}"));
         PasswordFactory factory = _factoryFor(handler);

         // When
         string password = factory.GeneratePassword(1, alphabet, checkIfLeaked: true);

         // Then
         _ = password.Should().BeEmpty();
         _ = handler.HibpRequestCount.Should().Be(1);
         _ = handler.XonRequestCount.Should().Be(0);
      }

      [TestMethod]
      /*
       * An async leak check reuses the same prefix cache as the synchronous path.
      */
      public async Task Case12_PasswordLeakedAsync_UsesCache()
      {
         // Given
         const string password = "async-cache-password";
         string hash = _sha1Hex(password);
         RoutingHandler handler = new(
            hibp: _ => (HttpStatusCode.OK, $"{hash[5..]}:1\r\n"),
            xon: _ => (HttpStatusCode.OK, "{\"SearchPassAnon\":{}}"));
         PasswordFactory factory = _factoryFor(handler);

         // When
         bool first = await factory.PasswordLeakedAsync(password);
         bool second = await factory.PasswordLeakedAsync(password);

         // Then
         _ = first.Should().BeTrue();
         _ = second.Should().BeTrue();
         _ = handler.HibpRequestCount.Should().Be(1);
         _ = handler.XonRequestCount.Should().Be(0);
      }

      [TestMethod]
      /*
       * When HIBP is down, XposedOrNot answers the check and its result is cached
       * by Keccak prefix so a second call does not hit the network again.
      */
      public void Case13_PasswordLeaked_FallsBackToXonWhenHibpFails()
      {
         // Given
         const string password = "xon-failover-password";
         RoutingHandler handler = new(
            hibp: _ => (HttpStatusCode.ServiceUnavailable, null),
            xon: _ => (HttpStatusCode.OK, "{\"SearchPassAnon\":{\"count\":\"1\"}}"));
         PasswordFactory factory = _factoryFor(handler);

         // When
         bool first = factory.PasswordLeaked(password);
         bool second = factory.PasswordLeaked(password);

         // Then: HIBP failures are never cached (so a later recovery is possible),
         // but the definitive XON answer is, so the second call asks HIBP again
         // and skips XON.
         _ = first.Should().BeTrue();
         _ = second.Should().BeTrue();
         _ = handler.HibpRequestCount.Should().Be(2);
         _ = handler.XonRequestCount.Should().Be(1);
         _ = factory.CachedXonPrefixCount.Should().Be(1);
      }

      [TestMethod]
      /*
       * An XON 404 is a definitive "not leaked" and must be cached the same way
       * as a positive hit, so we do not keep probing after HIBP stays down.
      */
      public void Case14_PasswordLeaked_XonNotFoundIsCachedAsSafe()
      {
         // Given
         RoutingHandler handler = new(
            hibp: _ => (HttpStatusCode.ServiceUnavailable, null),
            xon: _ => (HttpStatusCode.NotFound, "{\"Error\":\"Not found\"}"));
         PasswordFactory factory = _factoryFor(handler);

         // When
         bool first = factory.PasswordLeaked("fresh-random-looking-password");
         bool second = factory.PasswordLeaked("fresh-random-looking-password");

         // Then
         _ = first.Should().BeFalse();
         _ = second.Should().BeFalse();
         _ = handler.HibpRequestCount.Should().Be(2);
         _ = handler.XonRequestCount.Should().Be(1);
         _ = factory.CachedXonPrefixCount.Should().Be(1);
      }

      [TestMethod]
      /*
       * Keccak-512 vectors published by XposedOrNot must match so the failover
       * prefix we send is the one their API indexes.
      */
      public void Case15_Keccak512_MatchesXposedOrNotVectors()
      {
         _ = Keccak512.HashHex("test").Should().Be(
            "1E2E9FC2002B002D75198B7503210C05A1BAAC4560916A3C6D93BCCE3A50D7F00FD395BF1647B9ABB8D1AFCC9C76C289B0C9383BA386A956DA4B38934417789E");
         _ = Keccak512.HashHex("pass").Should().Be(
            "ADF34F3E63A8E0BD2938F3E09DDC161125A031C3C86D06EC59574A5C723E7FDBE04C2C15D9171E05E90A9C822936185F12B9D7384B2BEDB02E75C4C5FE89E4D4");
      }

      [TestMethod]
      /*
       * Inputs longer than the Keccak rate (72 bytes) still produce a 512-bit
       * digest, exercising the multi-block absorb loop.
      */
      public void Case16_Keccak512_MultiBlockInput()
      {
         string longInput = new('a', 200);
         string digest = Keccak512.HashHex(longInput);

         _ = digest.Should().HaveLength(128);
         _ = digest.Should().NotBe(Keccak512.HashHex("a"));
         _ = Keccak512.Hash(Encoding.UTF8.GetBytes(longInput)).Should().HaveCount(64);

         Action fromNull = () => Keccak512.HashHex(null!);
         fromNull.Should().Throw<ArgumentNullException>();
      }

      [TestMethod]
      /*
       * When HIBP succeeds, the offline Bloom filter must not be consulted.
      */
      public void Case17_PasswordLeaked_DoesNotQueryBloomWhenHibpSucceeds()
      {
         const string password = "hibp-wins-over-bloom";
         string hash = _sha1Hex(password);
         RecordingBloom bloom = new(mightContain: true);
         RoutingHandler handler = new(
            hibp: _ => (HttpStatusCode.OK, $"{hash[5..]}:1\r\n"),
            xon: _ => (HttpStatusCode.OK, "{\"SearchPassAnon\":{}}"));
         PasswordFactory factory = _factoryFor(handler, bloom);

         _ = factory.PasswordLeaked(password).Should().BeTrue();
         _ = bloom.QueryCount.Should().Be(0);
         _ = handler.XonRequestCount.Should().Be(0);
      }

      [TestMethod]
      /*
       * After HIBP and XON fail, a Bloom miss is a definitive "not leaked".
      */
      public void Case18_PasswordLeaked_BloomMissAfterNetworkFailure()
      {
         RecordingBloom bloom = new(mightContain: false);
         RoutingHandler handler = new(
            hibp: _ => (HttpStatusCode.ServiceUnavailable, null),
            xon: _ => (HttpStatusCode.ServiceUnavailable, null));
         PasswordFactory factory = _factoryFor(handler, bloom);

         _ = factory.PasswordLeaked("offline-safe-password").Should().BeFalse();
         _ = bloom.QueryCount.Should().Be(1);
      }

      [TestMethod]
      /*
       * After HIBP and XON fail, a Bloom hit is treated as leaked (conservative).
      */
      public void Case19_PasswordLeaked_BloomHitAfterNetworkFailure()
      {
         RecordingBloom bloom = new(mightContain: true);
         RoutingHandler handler = new(
            hibp: _ => (HttpStatusCode.ServiceUnavailable, null),
            xon: _ => (HttpStatusCode.ServiceUnavailable, null));
         PasswordFactory factory = _factoryFor(handler, bloom);

         _ = factory.PasswordLeaked("offline-maybe-leaked").Should().BeTrue();
         _ = bloom.QueryCount.Should().Be(1);
      }

      private static PasswordFactory _factoryFor(RoutingHandler handler, ILocalLeakFilter? localFilter = null)
         => new(
            (request, cancellationToken) => handler.Invoke(request, cancellationToken),
            (request, cancellationToken) => handler.InvokeAsync(request, cancellationToken),
            localFilter);

      private static string _sha1Hex(string value)
         => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(value)));

      private sealed class RecordingBloom : ILocalLeakFilter
      {
         private readonly bool _mightContain;

         public RecordingBloom(bool mightContain) => _mightContain = mightContain;

         public int QueryCount;

         public string? Path => null;

         public DateTime BuiltUtc => DateTime.UnixEpoch;

         public ulong InsertedCount => 0;

         public bool MightContain(ReadOnlySpan<byte> sha1)
         {
            _ = Interlocked.Increment(ref QueryCount);
            return _mightContain;
         }

         public void Dispose()
         {
         }
      }

      /// <summary>
      /// Routes mock responses by host so HIBP and XposedOrNot failover can be
      /// asserted independently without touching the network.
      /// </summary>
      private sealed class RoutingHandler : HttpMessageHandler
      {
         private readonly Func<string, (HttpStatusCode Status, string? Body)> _hibp;
         private readonly Func<string, (HttpStatusCode Status, string? Body)> _xon;

         public int HibpRequestCount;
         public int XonRequestCount;

         public RoutingHandler(
            Func<string, (HttpStatusCode Status, string? Body)> hibp,
            Func<string, (HttpStatusCode Status, string? Body)> xon)
         {
            _hibp = hibp;
            _xon = xon;
         }

         public HttpResponseMessage Invoke(HttpRequestMessage request, CancellationToken cancellationToken)
            => Send(request, cancellationToken);

         public Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => SendAsync(request, cancellationToken);

         protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            => _respond(request);

         protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_respond(request));

         private HttpResponseMessage _respond(HttpRequestMessage request)
         {
            string host = request.RequestUri?.Host ?? string.Empty;
            string prefix = request.RequestUri?.Segments.LastOrDefault() ?? string.Empty;

            (HttpStatusCode status, string? body) = host.Contains("xposedornot", StringComparison.OrdinalIgnoreCase)
               ? _xonAnswer(prefix)
               : _hibpAnswer(prefix);

            HttpResponseMessage response = new(status);
            if (body is not null)
            {
               response.Content = new StringContent(body, Encoding.UTF8);
            }

            return response;
         }

         private (HttpStatusCode Status, string? Body) _hibpAnswer(string prefix)
         {
            _ = Interlocked.Increment(ref HibpRequestCount);
            return _hibp(prefix);
         }

         private (HttpStatusCode Status, string? Body) _xonAnswer(string prefix)
         {
            _ = Interlocked.Increment(ref XonRequestCount);
            return _xon(prefix);
         }
      }
   }
}
