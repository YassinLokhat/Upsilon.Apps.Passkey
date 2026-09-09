using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class SecuritySettingsWarningViewModel : INotifyPropertyChanged, ILanguageAware, IDisposable
   {
      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property so WPF can refresh Title on language change.")]
      public string Title => Strings.Format(nameof(Strings.Title_SecuritySettingsWarningsWindow), AppInfo.Title);

      public SecuritySettingsIssueItemViewModel[] Issues { get; private set; }

      public event PropertyChangedEventHandler? PropertyChanged;

      public SecuritySettingsWarningViewModel()
      {
         AppServices.Session.Warnings.NotifiedWarningsChanged += _warnings_NotifiedWarningsChanged;

         Issues = _loadIssues();
      }

      public void OnLanguageChanged()
         => _reloadIssues(alsoTitle: true);

      public void Dispose()
      {
         AppServices.Session.Warnings.NotifiedWarningsChanged -= _warnings_NotifiedWarningsChanged;
      }

      private void _warnings_NotifiedWarningsChanged(object? sender, EventArgs e)
         => _reloadIssues(alsoTitle: false);

      private void _reloadIssues(bool alsoTitle)
      {
         Issues = _loadIssues();
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Issues)));
         if (alsoTitle)
         {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
         }
      }

      private static SecuritySettingsIssueItemViewModel[] _loadIssues()
      {
         SecuritySettingsIssue vaultIssues = SecuritySettingsIssue.None;
         HostSecurityIssue hostIssues = HostSecurityIssue.None;

         foreach (IVaultSecuritySettingsWarning warning in AppServices.Session.Warnings
            .GetAllWarnings(WarningKinds.VaultSecuritySettings)
            .OfType<IVaultSecuritySettingsWarning>())
         {
            vaultIssues |= warning.Issues;
         }

         foreach (IHostSecuritySettingsWarning warning in AppServices.Session.Warnings
            .GetAllWarnings(WarningKinds.HostSecuritySettings)
            .OfType<IHostSecuritySettingsWarning>())
         {
            hostIssues |= warning.Issues;
         }

         return
         [
            .. _vaultItems(vaultIssues),
            .. _hostItems(hostIssues),
         ];
      }

      private static IEnumerable<SecuritySettingsIssueItemViewModel> _vaultItems(SecuritySettingsIssue issues)
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

         if (issues.HasFlag(SecuritySettingsIssue.NoAccountUpdateReminder))
         {
            yield return new(
               Strings.Label_SecuritySettings_NoAccountUpdateReminder,
               Strings.Msg_SecuritySettings_NoAccountUpdateReminder);
         }
      }

      private static IEnumerable<SecuritySettingsIssueItemViewModel> _hostItems(HostSecurityIssue issues)
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

   internal sealed class SecuritySettingsIssueItemViewModel(string title, string description)
   {
      public string Title { get; } = title;
      public string Description { get; } = description;
   }
}
