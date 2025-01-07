namespace Upsilon.Apps.Passkey.Core.Interfaces
{
   /// <summary>
   /// Represent an user.
   /// </summary>
   public interface IUser : IItem
   {
      /// <summary>
      /// The username.
      /// </summary>
      string Username { get; set; }

      /// <summary>
      /// The passkeys.
      /// </summary>
      string[] Passkeys { get; set; }

      /// <summary>
      /// The number of minutes of inactivity before auto-logout.
      /// </summary>
      int LogoutTimeout { get; set; }

      /// <summary>
      /// The number of second to keep existing passwords in the clipboard.
      /// </summary>
      int CleaningClipboardTimeout { get; set; }

      /// <summary>
      /// The list of the user's services.
      /// </summary>
      IEnumerable<IService> Services { get; }

      /// <summary>
      /// Add a new service to the user's services.
      /// </summary>
      /// <param name="serviceName">The name of the new service.</param>
      void AddService(string serviceName);

      /// <summary>
      /// Delete the given service from the user's services. 
      /// </summary>
      /// <param name="serviceId">The Id of the service to delete.</param>
      void DeleteService(string serviceId);
   }
}
