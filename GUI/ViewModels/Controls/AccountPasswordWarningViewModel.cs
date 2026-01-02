using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   internal class AccountPasswordWarningViewModel(IAccount account, WarningType warningType)
   {
      public string ReadableWarningType => WarningType.ToReadableString();
      public string ServiceString => Account.Service.ToString() ?? string.Empty;
      public string AccountString => Account.ToString() ?? string.Empty;

      public IAccount Account = account;
      public WarningType WarningType { get; } = warningType;

      public bool MeetsConditions(WarningType warningType, string text)
      {
         return warningType.ContainsFlag(WarningType)
            && (AccountString.Contains(text, StringComparison.CurrentCultureIgnoreCase)
               || ServiceString.Contains(text, StringComparison.CurrentCultureIgnoreCase));
      }
   }
}
