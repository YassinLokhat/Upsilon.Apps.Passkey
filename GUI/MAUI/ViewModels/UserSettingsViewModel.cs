using System.Collections.ObjectModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;
using Upsilon.Apps.Passkey.GUI.MAUI.Themes;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class UserSettingsViewModel : ObservableObject
   {
      public UserSettingsViewModel()
      {
         LanguageCodes =
         [
            string.Empty,
            .. LocalizationService.Shipped.Select(s => s.Code),
         ];
         ThemeCodes =
         [
            string.Empty,
            ThemeService.SystemCode,
            ThemeService.LightCode,
            ThemeService.DarkCode,
         ];

         IUser? user = AppServices.Session.User;
         if (user is not null)
         {
            Username = user.Username;
            foreach (string passkey in user.Passkeys)
            {
               Passkeys.Add(new PasskeyEntry { Value = passkey });
            }

            LogoutTimeout = user.Settings.LogoutTimeout;
            CleaningClipboardTimeout = user.Settings.CleaningClipboardTimeout;
            ShowPasswordDelay = user.Settings.ShowPasswordDelay;
            NumberOfOldPasswordToKeep = user.Settings.NumberOfOldPasswordToKeep;
            NumberOfMonthActivitiesToKeep = user.Settings.NumberOfMonthActivitiesToKeep;
            SelectedLanguage = user.Settings.Language ?? string.Empty;
            SelectedTheme = user.Settings.Theme ?? string.Empty;
            NotifyActivityReview = (user.Settings.WarningsToNotify & WarningType.ActivityReviewWarning) != 0;
            NotifyPasswordUpdateReminder = (user.Settings.WarningsToNotify & WarningType.PasswordUpdateReminderWarning) != 0;
            NotifyDuplicatedPasswords = (user.Settings.WarningsToNotify & WarningType.DuplicatedPasswordsWarning) != 0;
            NotifyPasswordLeaked = (user.Settings.WarningsToNotify & WarningType.PasswordLeakedWarning) != 0;
         }

         if (Passkeys.Count == 0)
         {
            Passkeys.Add(new PasskeyEntry());
         }

         SaveCommand = new AsyncRelayCommand(_saveAsync);
         ImportCommand = new AsyncRelayCommand(_importAsync);
         ExportJsonCommand = new AsyncRelayCommand(() => _exportAsync(".json"));
         ExportCsvCommand = new AsyncRelayCommand(() => _exportAsync(".csv"));
         DeleteUserCommand = new AsyncRelayCommand(_deleteUserAsync);
         AddPasskeyCommand = new RelayCommand(() => Passkeys.Add(new PasskeyEntry()));
         RemovePasskeyCommand = new RelayCommand(p =>
         {
            if (p is PasskeyEntry entry && Passkeys.Count > 1)
            {
               _ = Passkeys.Remove(entry);
            }
         });
         BackCommand = new AsyncRelayCommand(() => AppServices.Navigation.GoBackAsync());
      }

      public string Title => Strings.Format(nameof(Strings.Title_UserSettings), PasskeyAppInfo.Title);

      public string Username
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public ObservableCollection<PasskeyEntry> Passkeys { get; } = [];

      public int LogoutTimeout
      {
         get;
         set => SetProperty(ref field, value);
      } = 5;

      public int CleaningClipboardTimeout
      {
         get;
         set => SetProperty(ref field, value);
      } = 30;

      public int ShowPasswordDelay
      {
         get;
         set => SetProperty(ref field, value);
      } = 500;

      public int NumberOfOldPasswordToKeep
      {
         get;
         set => SetProperty(ref field, value);
      }

      public int NumberOfMonthActivitiesToKeep
      {
         get;
         set => SetProperty(ref field, value);
      }

      public bool NotifyActivityReview
      {
         get;
         set => SetProperty(ref field, value);
      } = true;

      public bool NotifyPasswordUpdateReminder
      {
         get;
         set => SetProperty(ref field, value);
      } = true;

      public bool NotifyDuplicatedPasswords
      {
         get;
         set => SetProperty(ref field, value);
      } = true;

      public bool NotifyPasswordLeaked
      {
         get;
         set => SetProperty(ref field, value);
      } = true;

      public IReadOnlyList<string> LanguageCodes { get; }

      public IReadOnlyList<string> ThemeCodes { get; }

      public string SelectedLanguage
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string SelectedTheme
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public ICommand SaveCommand { get; }
      public ICommand ImportCommand { get; }
      public ICommand ExportJsonCommand { get; }
      public ICommand ExportCsvCommand { get; }
      public ICommand DeleteUserCommand { get; }
      public ICommand AddPasskeyCommand { get; }
      public ICommand RemovePasskeyCommand { get; }
      public ICommand BackCommand { get; }

      private string? _validate()
      {
         if (string.IsNullOrWhiteSpace(Username))
         {
            return Strings.Msg_UsernameEmpty;
         }

         if (Passkeys.Count == 0 || Passkeys.All(p => string.IsNullOrEmpty(p.Value)))
         {
            return Strings.Msg_AtLeastOnePassword;
         }

         if (Passkeys.Any(p => string.IsNullOrEmpty(p.Value)))
         {
            return Strings.Msg_NoPasswordEmpty;
         }

         return null;
      }

      private async Task _saveAsync()
      {
         IDatabase? database = AppServices.Session.Database;
         IUser? user = database?.User;
         if (database is null || user is null)
         {
            return;
         }

         string? error = _validate();
         if (error is not null)
         {
            await AppServices.Dialogs.WarnAsync(error, Strings.Title_Error).ConfigureAwait(true);
            return;
         }

         string[] newPasskeys = Passkeys.Select(p => p.Value).ToArray();
         string oldHash = AppServices.Cryptography.GetHash(user.Username);
         string newHash = AppServices.Cryptography.GetHash(Username.Trim());
         bool credentialsChanged = oldHash != newHash
            || AppServices.Serialization.AreDifferent(user.Passkeys, newPasskeys);

         user.Username = Username.Trim();
         user.Passkeys = newPasskeys;
         user.Settings.LogoutTimeout = LogoutTimeout;
         user.Settings.CleaningClipboardTimeout = CleaningClipboardTimeout;
         user.Settings.ShowPasswordDelay = ShowPasswordDelay;
         user.Settings.NumberOfOldPasswordToKeep = NumberOfOldPasswordToKeep;
         user.Settings.NumberOfMonthActivitiesToKeep = NumberOfMonthActivitiesToKeep;
         user.Settings.Language = SelectedLanguage;
         user.Settings.Theme = SelectedTheme;

         WarningType warnings = 0;
         if (NotifyActivityReview)
         {
            warnings |= WarningType.ActivityReviewWarning;
         }

         if (NotifyPasswordUpdateReminder)
         {
            warnings |= WarningType.PasswordUpdateReminderWarning;
         }

         if (NotifyDuplicatedPasswords)
         {
            warnings |= WarningType.DuplicatedPasswordsWarning;
         }

         if (NotifyPasswordLeaked)
         {
            warnings |= WarningType.PasswordLeakedWarning;
         }

         user.Settings.WarningsToNotify = warnings;

         try
         {
            await database.SaveAsync().ConfigureAwait(true);
            AppServices.Session.ApplySessionLanguage();
            AppServices.Session.ApplySessionTheme();

            string message = credentialsChanged
               ? Strings.Format(nameof(Strings.Msg_CredentialsUpdated), Username)
               : Strings.Format(nameof(Strings.Msg_UserUpdated), Username);

            if (credentialsChanged)
            {
               AppServices.Session.EndSession();
               await AppServices.Navigation.GoToLoginAsync().ConfigureAwait(true);
            }

            await AppServices.Dialogs.InfoAsync(message, Strings.Title_Success).ConfigureAwait(true);
         }
         catch (Exception ex)
            when (ex is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
         {
            await AppServices.Dialogs.WarnAsync(ex.Message, Strings.Title_Error).ConfigureAwait(true);
         }
      }

      private async Task _deleteUserAsync()
      {
         IDatabase? database = AppServices.Session.Database;
         if (database?.User is null)
         {
            return;
         }

         if (!await AppServices.Dialogs
               .ConfirmAsync(Strings.Msg_DeleteUserConfirm1, Strings.Title_ConfirmationRequired)
               .ConfigureAwait(true))
         {
            return;
         }

         ConfirmThreeWayResult second = await AppServices.Dialogs
            .ConfirmThreeWayAsync(Strings.Msg_DeleteUserConfirm2, Strings.Title_ConfirmationRequired)
            .ConfigureAwait(true);
         if (second != ConfirmThreeWayResult.Yes)
         {
            return;
         }

         string deletedName = Username;
         database.Delete();
         AppServices.Session.EndSession();
         await AppServices.Dialogs
            .InfoAsync(Strings.Format(nameof(Strings.Msg_UserDeleted), deletedName), Strings.Title_Success)
            .ConfigureAwait(true);
         await AppServices.Navigation.GoToLoginAsync().ConfigureAwait(true);
      }

      private async Task _importAsync()
      {
         IDatabase? database = AppServices.Session.Database;
         if (database?.User is null)
         {
            return;
         }

         if (!await _ensureSavedAsync(database, Strings.Msg_ImportData).ConfigureAwait(true))
         {
            return;
         }

         string? path = await AppServices.Dialogs
            .PickOpenFileAsync(Strings.Title_ImportData, "*")
            .ConfigureAwait(true);
         if (path is null)
         {
            return;
         }

         bool imported = await database.ImportFromFileAsync(path).ConfigureAwait(true);
         if (imported)
         {
            await AppServices.Dialogs.InfoAsync(Strings.Msg_ImportSuccess, Strings.Title_ImportSuccess)
               .ConfigureAwait(true);
         }
         else
         {
            await AppServices.Dialogs.WarnAsync(Strings.Msg_ImportFailed, Strings.Title_ImportFailed)
               .ConfigureAwait(true);
         }
      }

      private async Task _exportAsync(string extension)
      {
         IDatabase? database = AppServices.Session.Database;
         if (database?.User is null)
         {
            return;
         }

         if (!await _ensureSavedAsync(database, Strings.Msg_ExportData).ConfigureAwait(true))
         {
            return;
         }

         string title = extension == ".csv" ? Strings.Title_ExportCsv : Strings.Title_ExportJson;
         string suggested = Path.Join(
            PasskeyAppInfo.AppSettings.DefaultDatabaseDirectory,
            $"passkey-export{extension}");
         string? path = await AppServices.Dialogs
            .PickSaveFileAsync(title, suggested, extension.TrimStart('.'))
            .ConfigureAwait(true);
         if (path is null)
         {
            return;
         }

         if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
         {
            path += extension;
         }

         bool exported = await database.ExportToFileAsync(path).ConfigureAwait(true);
         if (exported)
         {
            await AppServices.Dialogs.InfoAsync(Strings.Msg_ExportSuccess, Strings.Title_ExportSuccess)
               .ConfigureAwait(true);
         }
         else
         {
            await AppServices.Dialogs.WarnAsync(Strings.Msg_ExportFailed, Strings.Title_ExportFailed)
               .ConfigureAwait(true);
         }
      }

      private static async Task<bool> _ensureSavedAsync(IDatabase database, string title)
      {
         if (!database.User!.HasChanged())
         {
            return true;
         }

         bool ok = await AppServices.Dialogs.ConfirmAsync(Strings.Msg_SaveBeforeContinue, title).ConfigureAwait(true);
         if (!ok)
         {
            return false;
         }

         await database.SaveAsync().ConfigureAwait(true);
         return true;
      }
   }
}
