using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class DuplicatedPasswordsWarningViewModel
   {
      public string Title { get; }

      public DuplicatedPasswordWarningViewModel[] Warnings { get; set; }

      public DuplicatedPasswordsWarningViewModel()
      {
         Title = Strings.Format(nameof(Strings.Title_DuplicatedPasswordsWarnings), AppInfo.Title);

         Warnings = [.. AppServices.Session.Database?.Warnings?
            .Where(x => x.WarningType == WarningType.DuplicatedPasswordsWarning)
            .Select(x => new DuplicatedPasswordWarningViewModel(x))
            ?? []];
      }
   }
}
