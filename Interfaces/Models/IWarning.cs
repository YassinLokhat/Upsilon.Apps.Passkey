using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IWarning
   {
      WarningType WarningType { get; }

      IEnumerable<IActivity>? Activities { get; }

      IEnumerable<IAccount>? Accounts { get; }

      /// <summary>
      /// Set when <see cref="WarningType"/> is
      /// <see cref="WarningType.SecuritySettingsWarning"/>; otherwise
      /// <see cref="SecuritySettingsIssue.None"/>.
      /// </summary>
      SecuritySettingsIssue SecuritySettingsIssues { get; }
   }
}
