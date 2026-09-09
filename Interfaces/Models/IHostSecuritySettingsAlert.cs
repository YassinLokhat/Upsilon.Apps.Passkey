using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>App-level posture issues published by the host (not the vault).</summary>
   public interface IHostSecuritySettingsAlert : IAlert
   {
      HostSecurityIssue Issues { get; }
   }
}
