using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class DuplicatedPasswordAlertViewModel
   {
      private readonly IAccountsAlert _warning;

      public string DuplicatedPassword => Strings.Format(nameof(Strings.Msg_DuplicatedPasswordAccounts), _warning.Accounts.Count());
      public AccountPasswordAlertViewModel[] Accounts { get; set; }

      public DuplicatedPasswordAlertViewModel(IAccountsAlert warning)
      {
         _warning = warning;
         Accounts = [.. _warning.Accounts.Select(x => new AccountPasswordAlertViewModel(x, _warning.Kind))];
      }
   }
}
