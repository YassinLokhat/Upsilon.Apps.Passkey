using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Upsilon.Apps.PassKey.Core.Enums;
using Upsilon.Apps.PassKey.Core.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Models
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
