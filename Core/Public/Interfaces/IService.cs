namespace Upsilon.Apps.Passkey.Core.Public.Interfaces
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
      IAccount[] Accounts { get; }

      /// <summary>
      /// Add a new account to this service.
      /// </summary>
      /// <param name="label">The label of the account.</param>
      /// <param name="identifiers">The identifiers.</param>
      /// <param name="password">The password.</param>
      /// <returns>The created account.</returns>
      IAccount AddAccount(string label, IEnumerable<string> identifiers, string password);

      /// <summary>
      /// Add a new account to this service.
      /// </summary>
      /// <param name="label">The label of the account.</param>
      /// <param name="identifiers">The identifiers.</param>
      /// <returns>The created account.</returns>
      IAccount AddAccount(string label, IEnumerable<string> identifiers);

      /// <summary>
      /// Add a new account to this service.
      /// </summary>
      /// <param name="identifiers">The identifiers.</param>
      /// <param name="password">The password.</param>
      /// <returns>The created account.</returns>
      IAccount AddAccount(IEnumerable<string> identifiers, string password);

      /// <summary>
      /// Add a new account to this service.
      /// </summary>
      /// <param name="identifiers">The identifiers.</param>
      /// <returns>The created account.</returns>
      IAccount AddAccount(IEnumerable<string> identifiers);

      /// <summary>
      /// Delete the given account from this service. 
      /// </summary>
      /// <param name="account">The account to delete.</param>
      void DeleteAccount(IAccount account);
   }
}
