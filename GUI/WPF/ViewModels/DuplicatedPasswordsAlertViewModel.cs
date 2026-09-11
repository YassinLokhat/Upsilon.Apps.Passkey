using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class DuplicatedPasswordsAlertViewModel : ObservableObject, ILanguageAware
   {
      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = Strings.Format(nameof(Strings.Title_DuplicatedPasswordsAlerts), AppInfo.Title);

      public DuplicatedPasswordAlertViewModel[] Alerts
      {
         get;
         private set => SetProperty(ref field, value);
      }

      public DuplicatedPasswordsAlertViewModel()
      {
         Alerts = _loadAlerts();
      }

      public void OnLanguageChanged()
      {
         Title = Strings.Format(nameof(Strings.Title_DuplicatedPasswordsAlerts), AppInfo.Title);
         Alerts = _loadAlerts();
      }

      private static DuplicatedPasswordAlertViewModel[] _loadAlerts()
         => [.. AppServices.Session.Alerts
            .GetNotifiedAlerts(AlertKinds.DuplicatedPasswords)
            .OfType<IAccountsAlert>()
            .Select(x => new DuplicatedPasswordAlertViewModel(x))];
   }
}
