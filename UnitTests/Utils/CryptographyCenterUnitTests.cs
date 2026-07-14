using FluentAssertions;
using System.Diagnostics;
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
         _ = UnitTestsHelper.CryptographicCenter.GetSlowHash(string.Empty, UnitTestsHelper.GetUsername());
         _stopwatch.Stop();

         // Then
         _ = _stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(500);
      }

      [TestMethod]
      /*
       * Hashing the same source with two different salts (usernames) yields two
       * different hashes, while the same source with the same salt is stable.
      */
      public void Case02_SlowHashSaltVariesPerUsername()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString();
            string firstUsername = UnitTestsHelper.GetRandomString();
            string secondUsername = firstUsername + "_other";

            // When
            string firstHash = UnitTestsHelper.CryptographicCenter.GetSlowHash(source, firstUsername);
            string firstHashAgain = UnitTestsHelper.CryptographicCenter.GetSlowHash(source, firstUsername);
            string secondHash = UnitTestsHelper.CryptographicCenter.GetSlowHash(source, secondUsername);

            // Then
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
   }
}
