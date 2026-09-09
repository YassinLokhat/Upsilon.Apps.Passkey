using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Warnings
{
   internal sealed class HostSecuritySettingsWarning : IHostSecuritySettingsWarning
   {
      public HostSecuritySettingsWarning(HostSecurityIssue issues)
         => Issues = issues;

      public string Source => WarningKinds.SourceHost;

      public string Kind => WarningKinds.HostSecuritySettings;

      public WarningSeverity Severity => WarningSeverity.Warning;

      public HostSecurityIssue Issues { get; }
   }
}
