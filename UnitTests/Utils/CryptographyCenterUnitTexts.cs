using FluentAssertions;
using Upsilon.Apps.PassKey.Core.Public.Utils;

namespace Upsilon.Apps.PassKey.UnitTests.Utils
{
   [TestClass]
   public sealed class CryptographyCenterUnitTexts
   {
      [TestMethod]
      /*
       * Signing an empty string returns the hash code of that empty string,
       * Then checking the signature returns the empty string.
      */
      public void Case01_SignEmptyString()
      {
         // Given
         string source = string.Empty;

         // When
         string signedSource = source;
         UnitTestsHelper.CryptographicCenter.Sign(ref signedSource);

         // Then
         _ = signedSource.Should().Be(UnitTestsHelper.CryptographicCenter.GetHash(string.Empty));

         // When
         string checkedSource = signedSource;
         _ = UnitTestsHelper.CryptographicCenter.CheckSign(ref checkedSource);

         // Then
         _ = checkedSource.Should().Be(source);
      }

      [TestMethod]
      /*
       * Signing a random string then check the sign should rise no error.
      */
      public void Case02_SignRandomString()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString();

            // When
            string signedSource = source;
            UnitTestsHelper.CryptographicCenter.Sign(ref signedSource);
            string checkedSource = signedSource;
            _ = UnitTestsHelper.CryptographicCenter.CheckSign(ref checkedSource);

            // Then
            _ = checkedSource.Should().Be(source);
         }
      }

      [TestMethod]
      /*
       * Encrypting symmetrically a random string then decrypting it should rise no error,
       * Then the decrypted string should be the same as the source.
      */
      public void Case03_SymmetricEncryptionRandomString()
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
      public void Case04_SymmetricEncryptionDecryptingCorruptedRandomString()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString();
            string[] passkeys = UnitTestsHelper.GetRandomStringArray();
            string encryptedSource = UnitTestsHelper.CryptographicCenter.EncryptSymmetrically(source, passkeys);
            string corruptedSource = encryptedSource + " ";
            CheckSignFailedException exception = null;

            // When
            Action act = new(() =>
            {
               try
               {
                  string decryptedSource = UnitTestsHelper.CryptographicCenter.DecryptSymmetrically(corruptedSource, passkeys);
               }
               catch (CheckSignFailedException ex)
               {
                  exception = ex;
                  throw;
               }
            });

            // Then
            _ = act.Should().Throw<CheckSignFailedException>();
            _ = exception.Should().NotBeNull();
         }
      }

      [TestMethod]
      /*
       * Decrypting symmetrically a random string with a wrong passkey should rise an error.
      */
      public void Case05_SymmetricEncryptionDecryptingRandomStringWithWrongPasskey()
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
      public void Case06_AsymmetricEncryptionRandomString()
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
      public void Case07_AsymmetricEncryptionDecryptingCorruptedRandomString()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString(150);
            UnitTestsHelper.CryptographicCenter.GenerateRandomKeys(out string publicKey, out string privateKey);
            string encryptedSource = UnitTestsHelper.CryptographicCenter.EncryptAsymmetrically(source, publicKey);
            string corruptedSource = encryptedSource + " ";
            CheckSignFailedException exception = null;

            // When
            Action act = new(() =>
            {
               try
               {
                  string decryptedSource = UnitTestsHelper.CryptographicCenter.DecryptAsymmetrically(corruptedSource, privateKey);
               }
               catch (CheckSignFailedException ex)
               {
                  exception = ex;
                  throw;
               }
            });

            // Then
            _ = act.Should().Throw<CheckSignFailedException>();
            _ = exception.Should().NotBeNull();
         }
      }

      [TestMethod]
      /*
       * Decrypting a random string with a wrong passkey should rise an error.
      */
      public void Case08_AsymmetricEncryptionDecryptingRandomStringWithWrongPasskey()
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
