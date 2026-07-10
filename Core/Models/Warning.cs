using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class Warning : IWarning
   {
      public WarningType WarningType { get; set; }

      public IEnumerable<IActivity>? Activities { get; set; }

      public IEnumerable<IAccount>? Accounts { get; set; }

      public Warning(IActivity[] activities)
      {
         WarningType = WarningType.ActivityReviewWarning;
         Activities = activities;
      }

      public Warning(WarningType warningType, IAccount[] accounts)
      {
         WarningType = warningType;
         Accounts = accounts;
      }
   }
}
