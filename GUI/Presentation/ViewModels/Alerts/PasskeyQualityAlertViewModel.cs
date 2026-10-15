using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using System.Windows.Input;
using Upsilon.Apps.Passkey.Interfaces.Models;


namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts
{
   internal sealed class PasskeyQualityAlertViewModel : ObservableObject, ILanguageRefreshable, IDisposable
   {
      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = Strings.Format(nameof(Strings.Title_PasskeyQualityAlertsWindow), AppServices.Info.Title);

      public IssueItemViewModel[] Issues
      {
         get;
         private set => SetProperty(ref field, value);
      }

      public ICommand OpenUserSettingsCommand { get; }
      public ICommand OpenAppSettingsCommand { get; }
      public ICommand OkCommand { get; }

      public event EventHandler? CloseRequested;

      /// <summary>Host opens the user settings view; raised before <see cref="CloseRequested"/>.</summary>
      public event EventHandler? OpenUserSettingsRequested;

      /// <summary>Host opens the application settings view; raised before <see cref="CloseRequested"/>.</summary>
      public event EventHandler? OpenAppSettingsRequested;

      public PasskeyQualityAlertViewModel()
      {
         OpenUserSettingsCommand = new RelayCommand(() =>
         {
            OpenUserSettingsRequested?.Invoke(this, EventArgs.Empty);
            CloseRequested?.Invoke(this, EventArgs.Empty);
         });
         OpenAppSettingsCommand = new RelayCommand(() =>
         {
            OpenAppSettingsRequested?.Invoke(this, EventArgs.Empty);
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
         OpenUserSettingsRequested = null;
         OpenAppSettingsRequested = null;
      }

      private void _alerts_NotifiedAlertsChanged(object? sender, EventArgs e)
         => UiThread.Post(() => _reloadIssues(alsoTitle: false));

      private void _reloadIssues(bool alsoTitle)
      {
         Issues = _loadIssues();
         if (alsoTitle)
         {
            Title = Strings.Format(nameof(Strings.Title_PasskeyQualityAlertsWindow), AppServices.Info.Title);
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

         foreach (string accounts in AppServices.Session.Alerts
            .GetNotifiedAlerts(AlertKinds.PasskeyReusedAsAccountPassword)
            .OfType<IPasskeyReuseAlert>()
            .Select(x => string.Join(", ", x.Accounts.Select(a => $"{a.Service} / {a}"))))
         {
            items.Add(new(Strings.Label_NotifyPasskeyReusedAsAccountPassword,
               Strings.Format(nameof(Strings.Msg_PasskeyQuality_PasskeyReused),
               accounts)));
         }

         return [.. items];
      }
   }
}
