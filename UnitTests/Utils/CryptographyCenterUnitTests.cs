using FluentAssertions;
using System.Diagnostics;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   [TestClass]
   public sealed class CryptographyCenterUnitTests
   {
      [TestMethod]
      /*
       * Signing an empty string returns the hash code of that empty string,
       * Then checking the signature returns the empty string.
      */
      public void Case01_SlowHash()
      {
         // Given
         Stopwatch _stopwatch = Stopwatch.StartNew();

         // When
         _ = UnitTestsHelper.CryptographicCenter.GetSlowHash(string.Empty, UnitTestsHelper.CryptographicCenter.DefaultSlowHashParameters);
         _stopwatch.Stop();

         // Then
         _ = _stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(500);
      }

      [TestMethod]
      /*
       * Each new set of parameters carries a fresh random salt, so the same
       * source hashed under two different salts yields two different hashes,
       * while re-hashing the same source under the same parameters is stable.
      */
      public void Case02_SlowHashSaltVariesPerParameters()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString();
            KdfParameters firstParameters = UnitTestsHelper.CryptographicCenter.DefaultSlowHashParameters;
            KdfParameters secondParameters = UnitTestsHelper.CryptographicCenter.DefaultSlowHashParameters;

            // When
            string firstHash = UnitTestsHelper.CryptographicCenter.GetSlowHash(source, firstParameters);
            string firstHashAgain = UnitTestsHelper.CryptographicCenter.GetSlowHash(source, firstParameters);
            string secondHash = UnitTestsHelper.CryptographicCenter.GetSlowHash(source, secondParameters);

            // Then
            _ = firstParameters.Salt.Should().NotBe(secondParameters.Salt);
            _ = firstHash.Should().Be(firstHashAgain);
            _ = firstHash.Should().NotBe(secondHash);
         }
      }

      [TestMethod]
      /*
       * The length of any should be constantly equal to `HashLength`.
      */
      public void Case03_HashLength()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString();

            // When
            string hash = UnitTestsHelper.CryptographicCenter.GetHash(source);

            // Then
            _ = hash.Length.Should().Be(UnitTestsHelper.CryptographicCenter.HashLength);
         }
      }

      [TestMethod]
      /*
       * Encrypting symmetrically a random string then decrypting it should rise no error,
       * Then the decrypted string should be the same as the source.
      */
      public void Case04_SymmetricEncryptionRandomString()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString();
            string[] passkeys = UnitTestsHelper.GetRandomStringArray();

            // When
            string encryptedSource = UnitTestsHelper.CryptographicCenter.EncryptSymmetrically(source, passkeys);
            string decryptedSource = UnitTestsHelper.CryptographicCenter.DecryptSymmetrically(encryptedSource, passkeys);

            // Then
            _ = decryptedSource.Should().Be(source);
         }
      }

      [TestMethod]
      /*
       * Decrypting symmetrically a corrupted string should rise an error.
      */
      public void Case05_SymmetricEncryptionDecryptingCorruptedRandomString()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString();
            string[] passkeys = UnitTestsHelper.GetRandomStringArray();
            string encryptedSource = UnitTestsHelper.CryptographicCenter.EncryptSymmetrically(source, passkeys);
            // Appending a character breaks the Base64 alignment of the
            // outermost layer (a lone space would be ignored by the decoder).
            string corruptedSource = encryptedSource + "A";
            CorruptedSourceException exception = null;

            // When
            Action act = new(() =>
            {
               try
               {
                  string decryptedSource = UnitTestsHelper.CryptographicCenter.DecryptSymmetrically(corruptedSource, passkeys);
               }
               catch (CorruptedSourceException ex)
               {
                  exception = ex;
                  throw;
               }
            });

            // Then
            _ = act.Should().Throw<CorruptedSourceException>();
            _ = exception.Should().NotBeNull();
         }
      }

      [TestMethod]
      /*
       * Decrypting symmetrically a random string with a wrong passkey should rise an error.
      */
      public void Case06_SymmetricEncryptionDecryptingRandomStringWithWrongPasskey()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString();
            string[] passkeys = UnitTestsHelper.GetRandomStringArray();
            string encryptedSource = UnitTestsHelper.CryptographicCenter.EncryptSymmetrically(source, passkeys);
            int wrongKeyIndex = UnitTestsHelper.GetRandomInt(passkeys.Length);
            passkeys[wrongKeyIndex] = UnitTestsHelper.GetRandomString();
            WrongPasswordException exception = null;

            // When
            Action act = new(() =>
            {
               try
               {
                  string decryptedSource = UnitTestsHelper.CryptographicCenter.DecryptSymmetrically(encryptedSource, passkeys);
               }
               catch (WrongPasswordException ex)
               {
                  exception = ex;
                  throw;
               }
            });

            // Then
            _ = act.Should().Throw<WrongPasswordException>();
            _ = exception.Should().NotBeNull();
            _ = (exception?.PasswordLevel.Should().Be(wrongKeyIndex));
         }
      }

      [TestMethod]
      /*
       * Encrypting a random string then decrypting it should rise no error,
       * Then the decrypted string should be the same as the source.
      */
      public void Case07_AsymmetricEncryptionRandomString()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString(150);
            UnitTestsHelper.CryptographicCenter.GenerateRandomKeys(out string publicKey, out string privateKey);

            // When
            string encryptedSource = UnitTestsHelper.CryptographicCenter.EncryptAsymmetrically(source, publicKey);
            string decryptedSource = UnitTestsHelper.CryptographicCenter.DecryptAsymmetrically(encryptedSource, privateKey);

            // Then
            _ = decryptedSource.Should().Be(source);
         }
      }

      [TestMethod]
      /*
       * Decrypting a corrupted string should rise an error.
      */
      public void Case08_AsymmetricEncryptionDecryptingCorruptedRandomString()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString(150);
            UnitTestsHelper.CryptographicCenter.GenerateRandomKeys(out string publicKey, out string privateKey);
            string encryptedSource = UnitTestsHelper.CryptographicCenter.EncryptAsymmetrically(source, publicKey);
            // Appending a character makes the JSON envelope invalid (a lone
            // space would be ignored by the JSON reader).
            string corruptedSource = encryptedSource + "A";
            CorruptedSourceException exception = null;

            // When
            Action act = new(() =>
            {
               try
               {
                  string decryptedSource = UnitTestsHelper.CryptographicCenter.DecryptAsymmetrically(corruptedSource, privateKey);
               }
               catch (CorruptedSourceException ex)
               {
                  exception = ex;
                  throw;
               }
            });

            // Then
            _ = act.Should().Throw<CorruptedSourceException>();
            _ = exception.Should().NotBeNull();
         }
      }

      [TestMethod]
      /*
       * Decrypting a random string with a wrong passkey should rise an error.
      */
      public void Case09_AsymmetricEncryptionDecryptingRandomStringWithWrongPasskey()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString(150);
            UnitTestsHelper.CryptographicCenter.GenerateRandomKeys(out string publicKey, out string privateKey);
            UnitTestsHelper.CryptographicCenter.GenerateRandomKeys(out string wrongPublicKey, out string wrongPrivateKey);
            string encryptedSource = UnitTestsHelper.CryptographicCenter.EncryptAsymmetrically(source, publicKey);
            WrongPasswordException exception = null;

            // When
            Action act = new(() =>
            {
               try
               {
                  string decryptedSource = UnitTestsHelper.CryptographicCenter.DecryptAsymmetrically(encryptedSource, wrongPrivateKey);
               }
               catch (WrongPasswordException ex)
               {
                  exception = ex;
                  throw;
               }
            });

            // Then
            _ = act.Should().Throw<WrongPasswordException>();
            _ = exception.Should().NotBeNull();
            _ = (exception?.PasswordLevel.Should().Be(0));
         }
      }

      [TestMethod]
      /*
       * The public key derived from a private key matches the one generated alongside it.
      */
      public void Case10_GetPublicKeyMatchesGeneratedPair()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            UnitTestsHelper.CryptographicCenter.GenerateRandomKeys(out string publicKey, out string privateKey);

            // When
            string derivedPublicKey = UnitTestsHelper.CryptographicCenter.GetPublicKey(privateKey);

            // Then
            _ = derivedPublicKey.Should().Be(publicKey);
         }
      }

      [TestMethod]
      /*
       * A signature verifies against its source and public key,
       * but fails for altered content, an altered signature, or a wrong key.
      */
      public void Case11_SignAndVerify()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString(150);
            UnitTestsHelper.CryptographicCenter.GenerateRandomKeys(out string publicKey, out string privateKey);
            UnitTestsHelper.CryptographicCenter.GenerateRandomKeys(out string wrongPublicKey, out _);

            // When
            string signature = UnitTestsHelper.CryptographicCenter.Sign(source, privateKey);

            // Then
            _ = UnitTestsHelper.CryptographicCenter.Verify(source, signature, publicKey).Should().BeTrue();
            _ = UnitTestsHelper.CryptographicCenter.Verify(source + "X", signature, publicKey).Should().BeFalse();
            _ = UnitTestsHelper.CryptographicCenter.Verify(source, signature, wrongPublicKey).Should().BeFalse();
            _ = UnitTestsHelper.CryptographicCenter.Verify(source, "not-a-signature", publicKey).Should().BeFalse();
         }
      }

      [TestMethod]
      /*
       * Default slow-hash parameters satisfy the KDF floor; weakened or
       * malformed parameters are rejected by Ensure and by GetSlowHash.
      */
      public void Case12_SlowHashKdfFloor()
      {
         ICryptographyCenter crypto = UnitTestsHelper.CryptographicCenter;
         KdfParameters defaults = crypto.DefaultSlowHashParameters;

         Action ensureDefaults = () => crypto.EnsureSufficientSlowHashParameters(defaults);
         ensureDefaults.Should().NotThrow();

         KdfParameters weakIterations = new()
         {
            Algorithm = defaults.Algorithm,
            Iterations = 1,
            OutputLength = defaults.OutputLength,
            Salt = defaults.Salt,
         };
         Action ensureWeakIterations = () => crypto.EnsureSufficientSlowHashParameters(weakIterations);
         ensureWeakIterations.Should().Throw<InsufficientKdfParametersException>()
            .WithMessage("*iterations*");
         Action hashWeakIterations = () => crypto.GetSlowHash("passkey", weakIterations);
         hashWeakIterations.Should().Throw<InsufficientKdfParametersException>();

         KdfParameters weakOutput = new()
         {
            Algorithm = defaults.Algorithm,
            Iterations = defaults.Iterations,
            OutputLength = 16,
            Salt = defaults.Salt,
         };
         Action ensureWeakOutput = () => crypto.EnsureSufficientSlowHashParameters(weakOutput);
         ensureWeakOutput.Should().Throw<InsufficientKdfParametersException>()
            .WithMessage("*output length*");

         KdfParameters shortSalt = new()
         {
            Algorithm = defaults.Algorithm,
            Iterations = defaults.Iterations,
            OutputLength = defaults.OutputLength,
            Salt = Convert.ToBase64String(new byte[8]),
         };
         Action ensureShortSalt = () => crypto.EnsureSufficientSlowHashParameters(shortSalt);
         ensureShortSalt.Should().Throw<InsufficientKdfParametersException>()
            .WithMessage("*salt*");

         KdfParameters badSalt = new()
         {
            Algorithm = defaults.Algorithm,
            Iterations = defaults.Iterations,
            OutputLength = defaults.OutputLength,
            Salt = "not-valid-base64!!!",
         };
         Action ensureBadSalt = () => crypto.EnsureSufficientSlowHashParameters(badSalt);
         ensureBadSalt.Should().Throw<InsufficientKdfParametersException>()
            .WithMessage("*Base64*");

         // OWASP floor for SHA-512 is accepted (below the create default, above the reject line).
         KdfParameters owaspFloor = new()
         {
            Algorithm = KdfAlgorithm.Pbkdf2HmacSha512,
            Iterations = 210_000,
            OutputLength = defaults.OutputLength,
            Salt = defaults.Salt,
         };
         Action ensureOwaspFloor = () => crypto.EnsureSufficientSlowHashParameters(owaspFloor);
         ensureOwaspFloor.Should().NotThrow();
      }
   }
}
