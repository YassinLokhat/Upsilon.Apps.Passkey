namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IUser : IItem
   {
      string Username { get; set; }

      /// <summary>
      /// Ordered master passkeys that protect the vault onion.
      /// </summary>
      IEnumerable<string> Passkeys { get; set; }

      ISettings Settings { get; set; }

      IEnumerable<IService> Services { get; }

      IService AddService(string serviceName);

      void DeleteService(IService service);
   }
}
