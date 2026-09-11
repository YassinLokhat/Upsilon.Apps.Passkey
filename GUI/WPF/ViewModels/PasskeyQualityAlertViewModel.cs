using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Views;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class PasskeyQualityAlertViewModel : ObservableObject, ILanguageAware, IDisposable
   {
      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = Strings.Format(nameof(Strings.Title_PasskeyQualityAlertsWindow), AppInfo.Title);

      public IssueItemViewModel[] Issues
      {
         get;
         private set => SetProperty(ref field, value);
      }

      public ICommand OpenUserSettingsCommand { get; }
      public ICommand OpenAppSettingsCommand { get; }
      public ICommand OkCommand { get; }

      public event EventHandler? CloseRequested;

      public PasskeyQualityAlertViewModel()
      {
         OpenUserSettingsCommand = new RelayCommand(() =>
         {
            UserSettingsView.ShowUserSettings();
            CloseRequested?.Invoke(this, EventArgs.Empty);
         });
         OpenAppSettingsCommand = new RelayCommand(() =>
         {
            AppSettingsView.ShowAppSettings();
            CloseRequested?.Invoke(this, EventArgs.Empty);
         });
         OkCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

         AppServices.Session.Alerts.NotifiedAlertsChanged += _alerts_NotifiedAlertsChanged;
         Issues = _loadIssues();
      }

      public void OnLanguageChanged()
         => _reloadIssues(alsoTitle: true);

      public void Dispose()
      {
         AppServices.Session.Alerts.NotifiedAlertsChanged -= _alerts_NotifiedAlertsChanged;
         CloseRequested = null;
      }

      private void _alerts_NotifiedAlertsChanged(object? sender, EventArgs e)
         => UiThread.Post(() => _reloadIssues(alsoTitle: false));

      private void _reloadIssues(bool alsoTitle)
      {
         Issues = _loadIssues();
         if (alsoTitle)
         {
            Title = Strings.Format(nameof(Strings.Title_PasskeyQualityAlertsWindow), AppInfo.Title);
         }
      }

      private static IssueItemViewModel[] _loadIssues()
      {
         List<IssueItemViewModel> items = [];

         foreach (IInsufficientPasskeysAlert warning in AppServices.Session.Alerts
            .GetNotifiedAlerts(AlertKinds.InsufficientPasskeys)
            .OfType<IInsufficientPasskeysAlert>())
         {
            items.Add(new(
               Strings.Label_NotifyInsufficientPasskeys,
               Strings.Format(nameof(Strings.Msg_PasskeyQuality_InsufficientPasskeys), warning.Count, warning.RecommendedMinimum)));
         }

         foreach (IWeakPasskeyAlert warning in AppServices.Session.Alerts
            .GetNotifiedAlerts(AlertKinds.WeakPasskey)
            .OfType<IWeakPasskeyAlert>())
         {
            string indexes = string.Join(", ", warning.PasskeyIndexes.Select(i => i + 1));
            items.Add(new(
               Strings.Label_NotifyWeakPasskey,
               Strings.Format(nameof(Strings.Msg_PasskeyQuality_WeakPasskey), indexes)));
         }

         foreach (IPasskeyLeakedAlert warning in AppServices.Session.Alerts
            .GetNotifiedAlerts(AlertKinds.PasskeyLeaked)
            .OfType<IPasskeyLeakedAlert>())
         {
            string indexes = string.Join(", ", warning.PasskeyIndexes.Select(i => i + 1));
            items.Add(new(
               Strings.Label_NotifyPasskeyLeaked,
               Strings.Format(nameof(Strings.Msg_PasskeyQuality_PasskeyLeaked), indexes)));
         }

         return [.. items];
      }
   }
}
