using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class AccountPasswordAlertViewModel
   {
      public string ReadableAlertKind => EnumHelper.ToReadableAlertKind(Kind);
      public string ServiceString => Account.Service.ToString() ?? string.Empty;
      public string AccountString => Account.ToString() ?? string.Empty;

      public readonly IAccount Account;
      public string Kind { get; }

      public ICommand GoToCommand { get; }

      public AccountPasswordAlertViewModel(IAccount account, string kind)
      {
         Account = account;
         Kind = kind;
         GoToCommand = new RelayCommand(() => AppServices.Navigation.RequestItem(Account.ItemId));
      }

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
