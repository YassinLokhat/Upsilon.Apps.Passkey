using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal class DuplicatedPasswordWarningViewModel
   {
      private readonly IWarning _warning;

      public string DuplicatedPassword => $"{_warning.Accounts?.Length} accounts with same passwords";
      public AccountPasswordWarningViewModel[] Accounts { get; set; }

      public DuplicatedPasswordWarningViewModel(IWarning warning)
      {
         _warning = warning;
         Accounts = [.. _warning.Accounts?.Select(x => new AccountPasswordWarningViewModel(x, _warning.WarningType)) ?? []];
      }
   }
}
