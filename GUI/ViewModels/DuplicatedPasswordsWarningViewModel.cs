using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
    internal class DuplicatedPasswordsWarningViewModel
   {
      public string Title { get; }

      public DuplicatedPasswordWarningViewModel[] Warnings { get; set; }

      public DuplicatedPasswordsWarningViewModel()
      {
         Title = MainViewModel.AppTitle + " - Duplicated Passwords Warnings";

         Warnings = [.. MainViewModel.Database?.Warnings?
            .Where(x => x.WarningType == WarningType.DuplicatedPasswordsWarning)
            .Select(x => new DuplicatedPasswordWarningViewModel(x))
            ?? []];
      }
   }
}
