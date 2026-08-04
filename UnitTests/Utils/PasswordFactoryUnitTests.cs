using FluentAssertions;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.Passkey.Core.Utils;

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
         CountingHandler handler = new(_ => $"{hash[5..]}:1\r\n");
         PasswordFactory factory = _factoryFor(handler);

         // When
         bool first = factory.PasswordLeaked(password);
         bool second = factory.PasswordLeaked(password);

         // Then
         _ = first.Should().BeTrue();
         _ = second.Should().BeTrue();
         _ = handler.RequestCount.Should().Be(1);
         _ = factory.CachedRangeCount.Should().Be(1);
      }

      [TestMethod]
      /*
       * A non-success HTTP response must not be cached, otherwise a transient
       * outage would pin "not leaked" for the rest of the process.
      */
      public void Case10_PasswordLeaked_DoesNotCacheHttpFailures()
      {
         // Given
         CountingHandler handler = new(_ => null, HttpStatusCode.ServiceUnavailable);
         PasswordFactory factory = _factoryFor(handler);

         // When
         bool first = factory.PasswordLeaked("any-password");
         bool second = factory.PasswordLeaked("any-password");

         // Then
         _ = first.Should().BeFalse();
         _ = second.Should().BeFalse();
         _ = handler.RequestCount.Should().Be(2);
         _ = factory.CachedRangeCount.Should().Be(0);
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
         CountingHandler handler = new(_ => $"{hash[5..]}:99\r\n");
         PasswordFactory factory = _factoryFor(handler);

         // When
         string password = factory.GeneratePassword(1, alphabet, checkIfLeaked: true);

         // Then
         _ = password.Should().BeEmpty();
         _ = handler.RequestCount.Should().Be(1);
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
         CountingHandler handler = new(_ => $"{hash[5..]}:1\r\n");
         PasswordFactory factory = _factoryFor(handler);

         // When
         bool first = await factory.PasswordLeakedAsync(password);
         bool second = await factory.PasswordLeakedAsync(password);

         // Then
         _ = first.Should().BeTrue();
         _ = second.Should().BeTrue();
         _ = handler.RequestCount.Should().Be(1);
      }

      private static PasswordFactory _factoryFor(CountingHandler handler)
         => new(
            (request, cancellationToken) => handler.Invoke(request, cancellationToken),
            (request, cancellationToken) => handler.InvokeAsync(request, cancellationToken));

      private static string _sha1Hex(string value)
#pragma warning disable CA5350 // Test helper mirroring the production HIBP SHA-1 requirement
         => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(value)));
#pragma warning restore CA5350

      /// <summary>
      /// Counts outbound HIBP-style range requests and returns a fixed body
      /// (or an error status) so leak-check behaviour can be asserted offline.
      /// </summary>
      private sealed class CountingHandler : HttpMessageHandler
      {
         private readonly Func<string, string?> _bodyFactory;
         private readonly HttpStatusCode _statusCode;

         public int RequestCount;

         public CountingHandler(Func<string, string?> bodyFactory, HttpStatusCode statusCode = HttpStatusCode.OK)
         {
            _bodyFactory = bodyFactory;
            _statusCode = statusCode;
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
            _ = Interlocked.Increment(ref RequestCount);

            string prefix = request.RequestUri?.Segments.LastOrDefault() ?? string.Empty;
            string? body = _bodyFactory(prefix);

            HttpResponseMessage response = new(_statusCode);

            if (body is not null)
            {
               response.Content = new StringContent(body, Encoding.UTF8);
            }

            return response;
         }
      }
   }
}
