using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Views;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class SecuritySettingsAlertViewModel : ObservableObject, ILanguageAware, IDisposable
   {
      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = Strings.Format(nameof(Strings.Title_SecuritySettingsAlertsWindow), AppInfo.Title);

      public IssueItemViewModel[] Issues
      {
         get;
         private set => SetProperty(ref field, value);
      }

      public ICommand OpenUserSettingsCommand { get; }
      public ICommand OpenAppSettingsCommand { get; }
      public ICommand OkCommand { get; }

      public event EventHandler? CloseRequested;

      public SecuritySettingsAlertViewModel()
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
            Title = Strings.Format(nameof(Strings.Title_SecuritySettingsAlertsWindow), AppInfo.Title);
         }
      }

      private static IssueItemViewModel[] _loadIssues()
      {
         SecuritySettingsIssue vaultIssues = SecuritySettingsIssue.None;
         HostSecurityIssue hostIssues = HostSecurityIssue.None;

         foreach (IVaultSecuritySettingsAlert warning in AppServices.Session.Alerts
            .GetNotifiedAlerts(AlertKinds.VaultSecuritySettings)
            .OfType<IVaultSecuritySettingsAlert>())
         {
            vaultIssues |= warning.Issues;
         }

         foreach (IHostSecuritySettingsAlert warning in AppServices.Session.Alerts
            .GetNotifiedAlerts(AlertKinds.HostSecuritySettings)
            .OfType<IHostSecuritySettingsAlert>())
         {
            hostIssues |= warning.Issues;
         }

         return
         [
            .. _vaultItems(vaultIssues),
            .. _hostItems(hostIssues),
         ];
      }

      private static IEnumerable<IssueItemViewModel> _vaultItems(SecuritySettingsIssue issues)
      {
         if (issues.HasFlag(SecuritySettingsIssue.AutoLogoutDisabled))
         {
            yield return new(
               Strings.Label_SecuritySettings_AutoLogoutDisabled,
               Strings.Msg_SecuritySettings_AutoLogoutDisabled);
         }

         if (issues.HasFlag(SecuritySettingsIssue.ClipboardCleaningDisabled))
         {
            yield return new(
               Strings.Label_SecuritySettings_ClipboardCleaningDisabled,
               Strings.Msg_SecuritySettings_ClipboardCleaningDisabled);
         }

         if (issues.HasFlag(SecuritySettingsIssue.QrAutoCloseDisabled))
         {
            yield return new(
               Strings.Label_SecuritySettings_QrAutoCloseDisabled,
               Strings.Msg_SecuritySettings_QrAutoCloseDisabled);
         }

         if (issues.HasFlag(SecuritySettingsIssue.NoAccountLeakCheck))
         {
            yield return new(
               Strings.Label_SecuritySettings_NoAccountLeakCheck,
               Strings.Msg_SecuritySettings_NoAccountLeakCheck);
         }

         if (issues.HasFlag(SecuritySettingsIssue.NoAccountDuplicateCheck))
         {
            yield return new(
               Strings.Label_SecuritySettings_NoAccountDuplicateCheck,
               Strings.Msg_SecuritySettings_NoAccountDuplicateCheck);
         }

         if (issues.HasFlag(SecuritySettingsIssue.NoAccountWeakPasswordCheck))
         {
            yield return new(
               Strings.Label_SecuritySettings_NoAccountWeakPasswordCheck,
               Strings.Msg_SecuritySettings_NoAccountWeakPasswordCheck);
         }

         if (issues.HasFlag(SecuritySettingsIssue.NoAccountUpdateReminder))
         {
            yield return new(
               Strings.Label_SecuritySettings_NoAccountUpdateReminder,
               Strings.Msg_SecuritySettings_NoAccountUpdateReminder);
         }
      }

      private static IEnumerable<IssueItemViewModel> _hostItems(HostSecurityIssue issues)
      {
         if (issues.HasFlag(HostSecurityIssue.IdleLoginDisabled))
         {
            yield return new(
               Strings.Label_SecuritySettings_IdleLoginDisabled,
               Strings.Msg_SecuritySettings_IdleLoginDisabled);
         }

         if (issues.HasFlag(HostSecurityIssue.OfflineLeakFilterUnavailable))
         {
            yield return new(
               Strings.Label_SecuritySettings_OfflineLeakFilterUnavailable,
               Strings.Msg_SecuritySettings_OfflineLeakFilterUnavailable);
         }
      }
   }
}
