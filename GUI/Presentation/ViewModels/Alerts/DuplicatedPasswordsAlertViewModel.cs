using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts
{
   internal sealed class DuplicatedPasswordsAlertViewModel : ObservableObject, ILanguageRefreshable
   {
      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = Strings.Format(nameof(Strings.Title_DuplicatedPasswordsAlerts), AppServices.Info.Title);

      public DuplicatedPasswordAlertViewModel[] Alerts
      {
         get;
         private set => SetProperty(ref field, value);
      }

      public DuplicatedPasswordAlertViewModel? SelectedAlert
      {
         get;
         set => SetProperty(ref field, value);
      }

      public DuplicatedPasswordsAlertViewModel()
      {
         Alerts = _loadAlerts();
         SelectedAlert = Alerts.FirstOrDefault();
      }

      public void OnLanguageChanged()
      {
         Title = Strings.Format(nameof(Strings.Title_DuplicatedPasswordsAlerts), AppServices.Info.Title);

         DuplicatedPasswordAlertViewModel? previous = SelectedAlert;
         Alerts = _loadAlerts();
         SelectedAlert = previous is not null
            ? Alerts.FirstOrDefault(w => w.Accounts.Length == previous.Accounts.Length
               && ReferenceEquals(w.Accounts.FirstOrDefault()?.Account, previous.Accounts.FirstOrDefault()?.Account))
              ?? Alerts.FirstOrDefault()
            : Alerts.FirstOrDefault();
      }

      private static DuplicatedPasswordAlertViewModel[] _loadAlerts()
         => [.. AppServices.Session.Alerts
            .GetNotifiedAlerts(AlertKinds.DuplicatedPasswords)
            .OfType<IAccountsAlert>()
            .Select(x => new DuplicatedPasswordAlertViewModel(x))];
   }
}
