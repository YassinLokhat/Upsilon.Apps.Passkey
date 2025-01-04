namespace Upsilon.Apps.Passkey.Core.Interfaces
{
   public interface IService : IItem
   {
      string ServiceName { get; set; }
      string Url { get; set; }
      string Notes { get; set; }
      IUser User { get; }
      IEnumerable<IAccount> Accounts { get; }

      void AddAccount(string label, IEnumerable<string> identifiants, string password);
      void DeleteAccount(string accountId);
   }
}
