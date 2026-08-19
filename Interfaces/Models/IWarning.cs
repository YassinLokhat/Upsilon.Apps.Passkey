using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IWarning
   {
      WarningType WarningType { get; }

      IEnumerable<IActivity>? Activities { get; }

      IEnumerable<IAccount>? Accounts { get; }
   }
}
