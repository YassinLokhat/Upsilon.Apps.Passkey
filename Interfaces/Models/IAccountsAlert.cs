namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>Alert whose payload is a set of accounts (reminder, leak, weak, …).</summary>
   public interface IAccountsAlert : IAlert
   {
      IEnumerable<IAccount> Accounts { get; }
   }

   public interface IPasswordUpdateReminderAlert : IAccountsAlert
   {
   }

   public interface IDuplicatedPasswordsAlert : IAccountsAlert
   {
   }

   public interface IPasswordLeakedAlert : IAccountsAlert
   {
   }

   public interface IWeakAccountPasswordAlert : IAccountsAlert
   {
   }

   public interface IPasskeyReuseAlert : IAccountsAlert
   {
   }
}
