﻿using FluentAssertions;
using Upsilon.Apps.Passkey.UnitTests;
using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.PassKey.UnitTests.Utils
{
   [TestClass]
   public sealed class SecurityCenterUnitTexts
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
       * Encrypting a random string then decrypting it should rise no error,
       * Then the decrypted string should be the same as the source.
      */
      public void Case03_EncryptionRandomString()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString();
            string[] passkeys = UnitTestsHelper.GetRandomStringArray();

            // When
            string encryptedSource = UnitTestsHelper.CryptographicCenter.Encrypt(source, passkeys);
            string decryptedSource = UnitTestsHelper.CryptographicCenter.Decrypt(encryptedSource, passkeys);

            // Then
            _ = decryptedSource.Should().Be(source);
         }
      }

      [TestMethod]
      /*
       * Decrypting a corrupted string should rise an error.
      */
      public void Case04_DecryptingCorruptedRandomString()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString();
            string[] passkeys = UnitTestsHelper.GetRandomStringArray();
            string encryptedSource = UnitTestsHelper.CryptographicCenter.Encrypt(source, passkeys);
            string corruptedSource = encryptedSource + " ";
            CheckSignFailedException? exception = null;

            // When
            Action act = new(() =>
            {
               try
               {
                  string decryptedSource = UnitTestsHelper.CryptographicCenter.Decrypt(corruptedSource, passkeys);
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
      public void Case05_DecryptingRandomStringWithWrongPasskey()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string source = UnitTestsHelper.GetRandomString();
            string[] passkeys = UnitTestsHelper.GetRandomStringArray();
            string encryptedSource = UnitTestsHelper.CryptographicCenter.Encrypt(source, passkeys);
            int wrongKeyIndex = UnitTestsHelper.GetRandomInt(passkeys.Length);
            passkeys[wrongKeyIndex] = UnitTestsHelper.GetRandomString();
            WrongPasswordException? exception = null;

            // When
            Action act = new(() =>
            {
               try
               {
                  string decryptedSource = UnitTestsHelper.CryptographicCenter.Decrypt(encryptedSource, passkeys);
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
   }
}
