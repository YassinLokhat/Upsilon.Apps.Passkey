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

      IEnumerable<IService> Services { get; set; }

      IService AddService(string serviceName);

      void DeleteService(IService service);

      /// <summary>
      /// Records a secret that was placed on the clipboard so the session
      /// clipboard-clean tick can scrub history for that value only, without
      /// revealing every stored account password.
      /// </summary>
      void RememberClipboardSecret(string text);
   }
}
