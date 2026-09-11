using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Alerts
{
   internal sealed class HostSecuritySettingsAlert(HostSecurityIssue issues) : IHostSecuritySettingsAlert
   {
      public string Source => AlertKinds.SourceHost;

      public string Kind => AlertKinds.HostSecuritySettings;

      public AlertSeverity Severity => AlertSeverity.Warning;

      public HostSecurityIssue Issues { get; } = issues;
   }
}
