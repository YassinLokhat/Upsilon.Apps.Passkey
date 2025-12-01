using Upsilon.Apps.Passkey.Core.Public.Enums;

namespace Upsilon.Apps.Passkey.Core.Public.Interfaces
{
   /// <summary>
   /// Represent an account.
   /// </summary>
   public interface IAccount : IItem
   {
      /// <summary>
      /// The service containing this account.
      /// </summary>
      IService Service { get; }

      /// <summary>
      /// The label.
      /// </summary>
      string Label { get; set; }

      /// <summary>
      /// The account's notes.
      /// </summary>
      string Notes { get; set; }

      /// <summary>
      /// The identifants.
      /// </summary>
      string[] Identifiers { get; set; }

      /// <summary>
      /// The actual password.
      /// </summary>
      string Password { get; set; }

      /// <summary>
      /// The passwords history.
      /// </summary>
      Dictionary<DateTime, string> Passwords { get; }

      /// <summary>
      /// The number of month before a password update remind.
      /// The value '0' means 'never'.
      /// </summary>
      int PasswordUpdateReminderDelay { get; set; }

      /// <summary>
      /// The account's options.
      /// </summary>
      AccountOption Options { get; set; }
   }
}