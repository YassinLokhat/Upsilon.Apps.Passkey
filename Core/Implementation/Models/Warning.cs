using Upsilon.Apps.PassKey.Core.Abstraction.Enums;
using Upsilon.Apps.PassKey.Core.Abstraction.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Implementation.Models
{
   internal class Warning : IWarning
   {
      public WarningType WarningType { get; set; }

      public ILog[]? Logs { get; set; }

      public IAccount[]? Accounts { get; set; }

      public Warning(ILog[] logs)
      {
         WarningType = WarningType.LogReviewWarning;
         Logs = logs;
      }

      public Warning(WarningType warningType, IAccount[] accounts)
      {
         WarningType = warningType;
         Accounts = accounts;
      }
   }
}
