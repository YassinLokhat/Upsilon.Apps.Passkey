namespace Upsilon.Apps.Passkey.Core.Interfaces
{
   /// <summary>
   /// Represent a service.
   /// </summary>
   public interface IService : IItem
   {
      /// <summary>
      /// The user containing this service.
      /// </summary>
      IUser User { get; }

      /// <summary>
      /// The service name.
      /// </summary>
      string ServiceName { get; set; }

      /// <summary>
      /// The service URL.
      /// </summary>
      string Url { get; set; }

      /// <summary>
      /// The service's notes.
      /// </summary>
      string Notes { get; set; }

      /// <summary>
      /// The list of the user's account on this service.
      /// </summary>
      IEnumerable<IAccount> Accounts { get; }

      /// <summary>
      /// Add a new account to this service.
      /// </summary>
      /// <param name="label">The label of the account.</param>
      /// <param name="identifiants">The identifiants.</param>
      /// <param name="password">The password.</param>
      void AddAccount(string label, IEnumerable<string> identifiants, string password);

      /// <summary>
      /// Delete the given account from this service. 
      /// </summary>
      /// <param name="accountId">The Id of the account to delete.</param>
      void DeleteAccount(string accountId);
   }
}
