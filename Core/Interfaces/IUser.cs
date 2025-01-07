namespace Upsilon.Apps.Passkey.Core.Interfaces
{
   public interface IUser : IItem
   {
      string Username { get; set; }
      string[] Passkeys { get; set; }
      int LogoutTimeout { get; set; }
      int CleaningClipboardTimeout { get; set; }
      IEnumerable<IService> Services { get; }

      void AddService(string serviceName);
      void DeleteService(string serviceId);
   }
}
