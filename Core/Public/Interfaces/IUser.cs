using Upsilon.Apps.Passkey.Core.Public.Enums;

namespace Upsilon.Apps.Passkey.Core.Public.Interfaces
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
      /// The delay to keep password visible.
      /// </summary>
      int ShowPasswordDelay { get; set; }

      /// <summary>
      /// The warnings types which will be notified if detected.
      /// </summary>
      WarningType WarningsToNotify { get; set; }

      /// <summary>
      /// The list of the user's services.
      /// </summary>
      IService[] Services { get; }

      /// <summary>
      /// Add a new service to the user's services.
      /// </summary>
      /// <param name="serviceName">The name of the new service.</param>
      /// <returns>The created service.</returns>
      IService AddService(string serviceName);

      /// <summary>
      /// Delete the given service from the user's services. 
      /// </summary>
      /// <param name="service">The service to delete.</param>
      void DeleteService(IService service);
   }
}
