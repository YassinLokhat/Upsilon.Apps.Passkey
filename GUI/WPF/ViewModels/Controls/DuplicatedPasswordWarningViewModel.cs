using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class DuplicatedPasswordWarningViewModel
   {
      private readonly IWarning _warning;

      public string DuplicatedPassword => Strings.Format(nameof(Strings.Msg_DuplicatedPasswordAccounts), _warning.Accounts?.Count());
      public AccountPasswordWarningViewModel[] Accounts { get; set; }

      public DuplicatedPasswordWarningViewModel(IWarning warning)
      {
         _warning = warning;
         Accounts = [.. _warning.Accounts?.Select(x => new AccountPasswordWarningViewModel(x, _warning.WarningType)) ?? []];
      }
   }
}
