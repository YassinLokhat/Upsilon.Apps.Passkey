using System.Text.RegularExpressions;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Heuristic typing for identifier values (CSV import and similar).
   /// Order: phone, then email, otherwise username.
   /// </summary>
   public static partial class IdentifierTypeDetector
   {
      public static IdentifierType Detect(string? value)
      {
         return string.IsNullOrWhiteSpace(value)
            ? IdentifierType.Username
            : _phoneRegex().IsMatch(value)
            ? IdentifierType.PhoneNumber
            : _mailRegex().IsMatch(value) ? IdentifierType.Email : IdentifierType.Username;
      }

      [GeneratedRegex(@"^\+\d{1,3}[\d\s\-\.]{6,20}$")]
      private static partial Regex _phoneRegex();

      [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
      private static partial Regex _mailRegex();
   }
}
