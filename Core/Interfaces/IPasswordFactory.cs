namespace Upsilon.Apps.PassKey.Core.Interfaces
{
   /// <summary>
   /// Represent a Password factory engine.
   /// </summary>
   public interface IPasswordFactory
   {
      /// <summary>
      /// The letters used by the factory.
      /// </summary>
      string Alphabetic { get; }

      /// <summary>
      /// The digits used by the factory.
      /// </summary>
      string Numeric { get; }

      /// <summary>
      /// The special characters used by the factory.
      /// </summary>
      string SpecialChars { get; }

      /// <summary>
      /// Generate a random password.
      /// </summary>
      /// <param name="length">The length of the password.</param>
      /// <param name="includeUpperCaseAlphabeticChars">Include the upper case letters.</param>
      /// <param name="includeLowerCaseAlphabeticChars">Include the lower case letters.</param>
      /// <param name="includeNumericChars">Include the digits.</param>
      /// <param name="includeSpecialChars">Include the special characters.</param>
      /// <param name="excludedChars">Exclude some specific characters.</param>
      /// <param name="onlySafePasswords">Ensure that the generated password has not been already leaked.</param>
      /// <returns>The random geenrated password.</returns>
      string GeneratePassword(int length,
         bool includeUpperCaseAlphabeticChars = true,
         bool includeLowerCaseAlphabeticChars = true,
         bool includeNumericChars = true,
         bool includeSpecialChars = true,
         string excludedChars = "",
         bool onlySafePasswords = true);

      /// <summary>
      /// Generate a random password.
      /// </summary>
      /// <param name="length">The length of the password.</param>
      /// <param name="alphabet">The alphabet used.</param>
      /// <param name="onlySafePasswords">Ensure that the generated password has not been already leaked.</param>
      /// <returns>The random geenrated password.</returns>
      string GeneratePassword(int length,
         string alphabet,
         bool onlySafePasswords = true);

      /// <summary>
      /// Check if the password has been leaked.
      /// </summary>
      /// <param name="password">The password to check.</param>
      /// <returns>Returns true if the password has been leaked.</returns>
      bool PasswordLeaked(string password);
   }
}
