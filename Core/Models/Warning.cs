using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// One warning bucket: either activities that need review, or accounts that
   /// share a leaked / duplicated / expired password.
   /// </summary>
   internal sealed class Warning : IWarning
   {
      #region IWarning interface implicit Internal

      public WarningType WarningType { get; set; }

      public IEnumerable<IActivity>? Activities { get; set; }

      public IEnumerable<IAccount>? Accounts { get; set; }

      #endregion

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
