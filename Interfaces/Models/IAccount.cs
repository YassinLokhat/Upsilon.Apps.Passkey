using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IAccount : IItem
   {
      IService Service { get; }

      string Label { get; set; }

      string Notes { get; set; }

      /// <summary>
      /// Logins, emails, or other identifiers for this account.
      /// </summary>
      IEnumerable<string> Identifiers { get; set; }

      string Password { get; set; }

      /// <summary>
      /// Dated password history (newest kept according to settings).
      /// </summary>
      Dictionary<DateTime, string> Passwords { get; }

      /// <summary>
      /// Months before a password-update reminder; <c>0</c> means never.
      /// </summary>
      int PasswordUpdateReminderDelay { get; set; }

      AccountOption Options { get; set; }
   }
}
