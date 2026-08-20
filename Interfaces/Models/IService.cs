namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IService : IItem
   {
      IUser User { get; }

      string ServiceName { get; set; }

      Uri? Url { get; set; }

      string Notes { get; set; }

      IEnumerable<IAccount> Accounts { get; }

      IAccount AddAccount(string label, IEnumerable<string> identifiers, string password);

      IAccount AddAccount(string label, IEnumerable<string> identifiers);

      IAccount AddAccount(IEnumerable<string> identifiers, string password);

      IAccount AddAccount(IEnumerable<string> identifiers);

      void DeleteAccount(IAccount account);
   }
}
