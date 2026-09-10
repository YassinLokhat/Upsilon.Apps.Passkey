using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class AccountPasswordAlertViewModel(IAccount account, string kind)
   {
      public string ReadableAlertKind => EnumHelper.ToReadableAlertKind(Kind);
      public string ServiceString => Account.Service.ToString() ?? string.Empty;
      public string AccountString => Account.ToString() ?? string.Empty;

      public readonly IAccount Account = account;
      public string Kind { get; } = kind;

      public bool MeetsConditions(string kindFilter, string text)
      {
         bool kindOk = EnumHelper.IsAccountPasswordFilterAll(kindFilter)
            ? EnumHelper.MatchesAccountPasswordKindFilter(Kind, kindFilter)
            : string.Equals(Kind, kindFilter, StringComparison.Ordinal);

         return kindOk
            && (AccountString.Contains(text, StringComparison.OrdinalIgnoreCase)
               || ServiceString.Contains(text, StringComparison.OrdinalIgnoreCase));
      }
   }
}
