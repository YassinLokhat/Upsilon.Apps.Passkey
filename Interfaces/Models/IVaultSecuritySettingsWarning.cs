using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IVaultSecuritySettingsWarning : IWarning
   {
      SecuritySettingsIssue Issues { get; }
   }
}
