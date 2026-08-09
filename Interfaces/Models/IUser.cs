using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
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
      IEnumerable<string> Passkeys { get; set; }

      /// <summary>
      /// The settings.
      /// </summary>
      ISettings Settings { get; set; }

      /// <summary>
      /// The list of the user's services.
      /// </summary>
      IEnumerable<IService> Services { get; }

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
