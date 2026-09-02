using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class SecuritySettingsWarningViewModel : INotifyPropertyChanged, ILanguageAware, IDisposable
   {
      private readonly IDatabase? _database;

      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property so WPF can refresh Title on language change.")]
      public string Title => Strings.Format(nameof(Strings.Title_SecuritySettingsWarningsWindow), AppInfo.Title);

      public SecuritySettingsIssueItemViewModel[] Issues { get; private set; }

      public event PropertyChangedEventHandler? PropertyChanged;

      public SecuritySettingsWarningViewModel()
      {
         _database = AppServices.Session.Database;
         _database?.WarningsUpdated += _database_WarningsUpdated;

         Issues = _loadIssues();
      }

      public void OnLanguageChanged()
         => _reloadIssues(alsoTitle: true);

      public void Dispose()
      {
         _database?.WarningsUpdated -= _database_WarningsUpdated;
      }

      private void _database_WarningsUpdated(object? sender, WarningsUpdatedEventArgs e)
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
         SecuritySettingsIssue issues = SecuritySettingsIssue.None;
         foreach (IWarning warning in AppServices.Session.Database?.Warnings ?? [])
         {
            if (warning.WarningType.HasFlag(WarningType.SecuritySettingsWarning))
            {
               issues |= warning.SecuritySettingsIssues;
            }
         }

         return
         [
            .. _items(issues),
         ];
      }

      private static IEnumerable<SecuritySettingsIssueItemViewModel> _items(SecuritySettingsIssue issues)
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

         if (issues.HasFlag(SecuritySettingsIssue.IdleLoginDisabled))
         {
            yield return new(
               Strings.Label_SecuritySettings_IdleLoginDisabled,
               Strings.Msg_SecuritySettings_IdleLoginDisabled);
         }

         if (issues.HasFlag(SecuritySettingsIssue.OfflineLeakFilterUnavailable))
         {
            yield return new(
               Strings.Label_SecuritySettings_OfflineLeakFilterUnavailable,
               Strings.Msg_SecuritySettings_OfflineLeakFilterUnavailable);
         }

         if (issues.HasFlag(SecuritySettingsIssue.DuplicatePasswordNotificationsDisabled))
         {
            yield return new(
               Strings.Label_SecuritySettings_DuplicatePasswordNotificationsDisabled,
               Strings.Msg_SecuritySettings_DuplicatePasswordNotificationsDisabled);
         }

         if (issues.HasFlag(SecuritySettingsIssue.PasswordUpdateReminderNotificationsDisabled))
         {
            yield return new(
               Strings.Label_SecuritySettings_PasswordUpdateReminderNotificationsDisabled,
               Strings.Msg_SecuritySettings_PasswordUpdateReminderNotificationsDisabled);
         }

         if (issues.HasFlag(SecuritySettingsIssue.PasswordLeakedNotificationsDisabled))
         {
            yield return new(
               Strings.Label_SecuritySettings_PasswordLeakedNotificationsDisabled,
               Strings.Msg_SecuritySettings_PasswordLeakedNotificationsDisabled);
         }
      }
   }

   internal sealed class SecuritySettingsIssueItemViewModel(string title, string description)
   {
      public string Title { get; } = title;
      public string Description { get; } = description;
   }
}
