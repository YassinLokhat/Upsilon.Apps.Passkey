using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Local structural quality checks for secrets (passkeys and account passwords).
   /// Does not create alerts — callers map the result onto their own types.
   /// </summary>
   public static class SecretQuality
   {
      public const int MinimumLength = 12;
      public const int MinimumCharacterClasses = 3;

      public static SecretQualityIssue Evaluate(string secret, string? username = null)
      {
         if (string.IsNullOrEmpty(secret))
         {
            return SecretQualityIssue.TooShort | SecretQualityIssue.LowDiversity;
         }

         SecretQualityIssue issues = SecretQualityIssue.None;

         if (secret.Length < MinimumLength)
         {
            issues |= SecretQualityIssue.TooShort;
         }

         int classes = 0;
         if (secret.Any(char.IsLower))
         {
            classes++;
         }

         if (secret.Any(char.IsUpper))
         {
            classes++;
         }

         if (secret.Any(char.IsDigit))
         {
            classes++;
         }

         if (secret.Any(c => !char.IsLetterOrDigit(c)))
         {
            classes++;
         }

         if (classes < MinimumCharacterClasses)
         {
            issues |= SecretQualityIssue.LowDiversity;
         }

         if (!string.IsNullOrEmpty(username)
            && secret.Equals(username, StringComparison.OrdinalIgnoreCase))
         {
            issues |= SecretQualityIssue.MatchesUsername;
         }

         if (_isTrivial(secret))
         {
            issues |= SecretQualityIssue.TrivialPattern;
         }

         return issues;
      }

      public static bool IsWeak(string secret, string? username = null)
         => Evaluate(secret, username) != SecretQualityIssue.None;

      private static bool _isTrivial(string secret)
      {
         if (secret.Distinct().Count() == 1)
         {
            return true;
         }

         bool ascending = true;
         bool descending = true;
         for (int i = 1; i < secret.Length; i++)
         {
            if (secret[i] != secret[i - 1] + 1)
            {
               ascending = false;
            }

            if (secret[i] != secret[i - 1] - 1)
            {
               descending = false;
            }
         }

         return ascending || descending;
      }
   }
}
