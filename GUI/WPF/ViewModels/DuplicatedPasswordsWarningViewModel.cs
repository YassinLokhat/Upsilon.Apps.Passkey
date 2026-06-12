using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class DuplicatedPasswordsWarningViewModel
   {
      public string Title { get; }

      public DuplicatedPasswordWarningViewModel[] Warnings { get; set; }

      public DuplicatedPasswordsWarningViewModel()
      {
         Title = AppInfo.Title + " - Duplicated Passwords Warnings";

         Warnings = [.. AppServices.Session.Database?.Warnings?
            .Where(x => x.WarningType == WarningType.DuplicatedPasswordsWarning)
            .Select(x => new DuplicatedPasswordWarningViewModel(x))
            ?? []];
      }
   }
}
