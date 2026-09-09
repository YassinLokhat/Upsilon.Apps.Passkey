using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IVaultSecuritySettingsAlert : IAlert
   {
      SecuritySettingsIssue Issues { get; }
   }
}
