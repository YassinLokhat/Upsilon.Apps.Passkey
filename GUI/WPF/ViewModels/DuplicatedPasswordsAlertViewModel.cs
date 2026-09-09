using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class DuplicatedPasswordsAlertViewModel : INotifyPropertyChanged, ILanguageAware
   {
      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property so WPF can refresh Title on language change.")]
      public string Title => Strings.Format(nameof(Strings.Title_DuplicatedPasswordsAlerts), AppInfo.Title);

      public DuplicatedPasswordAlertViewModel[] Alerts { get; private set; }

      public event PropertyChangedEventHandler? PropertyChanged;

      public DuplicatedPasswordsAlertViewModel()
      {
         Alerts = _loadAlerts();
      }

      public void OnLanguageChanged()
      {
         Alerts = _loadAlerts();
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Alerts)));
      }

      private static DuplicatedPasswordAlertViewModel[] _loadAlerts()
         => [.. AppServices.Session.Alerts
            .GetNotifiedAlerts(AlertKinds.DuplicatedPasswords)
            .OfType<IAccountsAlert>()
            .Select(x => new DuplicatedPasswordAlertViewModel(x))];
   }
}
