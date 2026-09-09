namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>Warning whose payload is a set of accounts (reminder, leak, weak, …).</summary>
   public interface IAccountsWarning : IWarning
   {
      IEnumerable<IAccount> Accounts { get; }
   }

   public interface IPasswordUpdateReminderWarning : IAccountsWarning
   {
   }

   public interface IDuplicatedPasswordsWarning : IAccountsWarning
   {
   }

   public interface IPasswordLeakedWarning : IAccountsWarning
   {
   }

   public interface IWeakAccountPasswordWarning : IAccountsWarning
   {
   }

   public interface IPasskeyReuseWarning : IAccountsWarning
   {
   }
}
