using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class AccountPasswordWarningViewModel(IAccount account, WarningType warningType)
   {
      public string ReadableWarningType => WarningType.ToReadableString();
      public string ServiceString => Account.Service.ToString() ?? string.Empty;
      public string AccountString => Account.ToString() ?? string.Empty;

      public readonly IAccount Account = account;
      public WarningType WarningType { get; } = warningType;

      public bool MeetsConditions(WarningType warningType, string text)
      {
         return warningType.HasFlag(WarningType)
            && (AccountString.Contains(text, StringComparison.OrdinalIgnoreCase)
               || ServiceString.Contains(text, StringComparison.OrdinalIgnoreCase));
      }
   }
}
