using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class DuplicatedPasswordsWarningViewModel : INotifyPropertyChanged, ILanguageAware
   {
      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property so WPF can refresh Title on language change.")]
      public string Title => Strings.Format(nameof(Strings.Title_DuplicatedPasswordsWarnings), AppInfo.Title);

      public DuplicatedPasswordWarningViewModel[] Warnings { get; private set; }

      public event PropertyChangedEventHandler? PropertyChanged;

      public DuplicatedPasswordsWarningViewModel()
      {
         Warnings = _loadWarnings();
      }

      public void OnLanguageChanged()
      {
         Warnings = _loadWarnings();
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Warnings)));
      }

      private static DuplicatedPasswordWarningViewModel[] _loadWarnings()
         => [.. AppServices.Session.Warnings
            .GetNotifiedWarnings(WarningKinds.DuplicatedPasswords)
            .OfType<IAccountsWarning>()
            .Select(x => new DuplicatedPasswordWarningViewModel(x))];
   }
}
