using Upsilon.Apps.Passkey.Core.Utils;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings
{
   /// <summary>
   /// User settings and vault lifecycle (create, save with credential change and
   /// rename, delete, import, export). Hosts open it after
   /// <see cref="ConfirmStoredCredentialsAsync"/> and close it on <see cref="CloseRequested"/>.
   /// </summary>
   internal sealed class UserSettingsViewModel : ObservableObject, ILanguageRefreshable, IDisposable
   {
      private bool _disposed;

      public UserSettingsViewModel()
      {
         IUser? user = AppServices.Session.Database?.User;
         IsNewUser = user is null;
         Passkeys = new PasskeyListViewModel(user);

         SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
         DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => !IsBusy && !IsNewUser);
         ImportCommand = new AsyncRelayCommand(ImportAsync, () => !IsBusy && !IsNewUser);
         ExportJsonCommand = new AsyncRelayCommand(() => ExportAsync(json: true), () => !IsBusy && !IsNewUser);
         ExportCsvCommand = new AsyncRelayCommand(() => ExportAsync(json: false), () => !IsBusy && !IsNewUser);
         ViewActivitiesCommand = new AsyncRelayCommand(_viewActivitiesAsync, () => !IsNewUser);

         SelectedLanguage = _languageFromSettings(user?.Settings.Language);
         SelectedTheme = _themeFromSettings(user?.Settings.Theme);

         if (user is null)
         {
            return;
         }

         Username = user.Username;
         LogoutTimeout = user.Settings.LogoutTimeout;
         CleaningClipboardTimeout = user.Settings.CleaningClipboardTimeout;
         ShowPasswordDelay = user.Settings.ShowPasswordDelay;
         NumberOfOldPasswordToKeep = user.Settings.NumberOfOldPasswordToKeep;
         NumberOfMonthActivitiesToKeep = user.Settings.NumberOfMonthActivitiesToKeep;

         AlertKindList notify = user.Settings.AlertsToNotify;
         NotifyActivityReview = notify.Contains(AlertKinds.ActivityReview);
         NotifyPasswordUpdateReminder = notify.Contains(AlertKinds.PasswordUpdateReminder);
         NotifyDuplicatedPasswords = notify.Contains(AlertKinds.DuplicatedPasswords);
         NotifyPasswordLeaked = notify.Contains(AlertKinds.PasswordLeaked);
         NotifySecuritySettings = notify.Contains(AlertKinds.VaultSecuritySettings)
            || notify.Contains(AlertKinds.HostSecuritySettings);
         NotifyInsufficientPasskeys = notify.Contains(AlertKinds.InsufficientPasskeys);
         NotifyWeakPasskey = notify.Contains(AlertKinds.WeakPasskey);
         NotifyPasskeyLeaked = notify.Contains(AlertKinds.PasskeyLeaked);
         NotifyWeakAccountPassword = notify.Contains(AlertKinds.WeakAccountPassword);
         NotifyPasskeyReusedAsAccountPassword = notify.Contains(AlertKinds.PasskeyReusedAsAccountPassword);
      }

      public bool IsNewUser { get; private set; }

      public PasskeyListViewModel Passkeys { get; }

      public ICommand SaveCommand { get; }
      public ICommand DeleteCommand { get; }
      public ICommand ImportCommand { get; }
      public ICommand ExportJsonCommand { get; }
      public ICommand ExportCsvCommand { get; }
      public ICommand ViewActivitiesCommand { get; }

      /// <summary>
      /// Raised before the session ends (credential change, creation, delete) or
      /// after a plain save; the host closes the settings view synchronously.
      /// </summary>
      public event EventHandler? CloseRequested;

      public bool IsBusy
      {
         get;
         private set
         {
            if (SetProperty(ref field, value))
            {
               foreach (ICommand command in (ICommand[])[SaveCommand, DeleteCommand, ImportCommand, ExportJsonCommand, ExportCsvCommand])
               {
                  (command as AsyncRelayCommand)?.RaiseCanExecuteChanged();
               }
            }
         }
      }

      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = _buildTitle();

      public string Username
      {
         get;
         set => SetProperty(ref field, value);
      } = Strings.Label_NewUser;

      public int LogoutTimeout
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(LogoutTimeoutChecked));
            }
         }
      } = 5;

      public bool LogoutTimeoutChecked
      {
         get => LogoutTimeout != 0;
         set
         {
            if (LogoutTimeoutChecked != value)
            {
               LogoutTimeout = value ? 5 : 0;
            }
         }
      }

      public int CleaningClipboardTimeout
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(CleaningClipboardTimeoutChecked));
            }
         }
      } = 30;

      public bool CleaningClipboardTimeoutChecked
      {
         get => CleaningClipboardTimeout != 0;
         set
         {
            if (CleaningClipboardTimeoutChecked != value)
            {
               CleaningClipboardTimeout = value ? 30 : 0;
            }
         }
      }

      public int ShowPasswordDelay
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(ShowPasswordDelayChecked));
            }
         }
      } = 500;

      public bool ShowPasswordDelayChecked
      {
         get => ShowPasswordDelay != 0;
         set
         {
            if (ShowPasswordDelayChecked != value)
            {
               ShowPasswordDelay = value ? 500 : 0;
            }
         }
      }

      public int NumberOfOldPasswordToKeep
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(NumberOfOldPasswordToKeepChecked));
            }
         }
      }

      public bool NumberOfOldPasswordToKeepChecked
      {
         get => NumberOfOldPasswordToKeep != 0;
         set
         {
            if (NumberOfOldPasswordToKeepChecked != value)
            {
               NumberOfOldPasswordToKeep = value ? 10 : 0;
            }
         }
      }

      public int NumberOfMonthActivitiesToKeep
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(NumberOfMonthActivitiesToKeepChecked));
            }
         }
      }

      public bool NumberOfMonthActivitiesToKeepChecked
      {
         get => NumberOfMonthActivitiesToKeep != 0;
         set
         {
            if (NumberOfMonthActivitiesToKeepChecked != value)
            {
               NumberOfMonthActivitiesToKeep = value ? 12 : 0;
            }
         }
      }

      public bool NotifyActivityReview { get; set => SetProperty(ref field, value); } = true;
      public bool NotifyPasswordUpdateReminder { get; set => SetProperty(ref field, value); } = true;
      public bool NotifyDuplicatedPasswords { get; set => SetProperty(ref field, value); } = true;
      public bool NotifyPasswordLeaked { get; set => SetProperty(ref field, value); } = true;
      public bool NotifySecuritySettings { get; set => SetProperty(ref field, value); } = true;
      public bool NotifyInsufficientPasskeys { get; set => SetProperty(ref field, value); } = true;
      public bool NotifyWeakPasskey { get; set => SetProperty(ref field, value); } = true;
      public bool NotifyPasskeyLeaked { get; set => SetProperty(ref field, value); } = true;
      public bool NotifyWeakAccountPassword { get; set => SetProperty(ref field, value); } = true;
      public bool NotifyPasskeyReusedAsAccountPassword { get; set => SetProperty(ref field, value); } = true;

      public IReadOnlyList<AppLanguage> Languages
      {
         get;
         private set => SetProperty(ref field, value);
      } = _buildLanguages();

      public AppLanguage SelectedLanguage
      {
         get;
         set
         {
            if (value is not null)
            {
               _ = SetProperty(ref field, value);
            }
         }
      }

      public IReadOnlyList<AppThemeOption> Themes
      {
         get;
         private set => SetProperty(ref field, value);
      } = _buildThemes();

      public AppThemeOption SelectedTheme
      {
         get;
         set
         {
            if (value is not null)
            {
               _ = SetProperty(ref field, value);
            }
         }
      }

      private static IDialogService _dialogs
         => AppServices.Dialogs ?? throw new InvalidOperationException("No dialog service registered.");

      private static IFileExchange _files => AppServices.Files ?? new PathFileExchange(_dialogs);

      private static readonly string[] _importExtensions = [".json", ".csv"];

      /// <summary>
      /// Re-type the vault's current username and ordered passkeys. Required
      /// before opening settings on an unlocked vault, changing credentials and deleting.
      /// </summary>
      public static async Task<bool> ConfirmStoredCredentialsAsync()
      {
         if (AppServices.Session.Database?.User is not { } user)
         {
            return false;
         }

         string[] credentials = [user.Username, .. user.Passkeys];
         try
         {
            return await _dialogs.ShowAsync(new ConfirmCredentialsRequest(credentials, IsNew: false)).ConfigureAwait(true);
         }
         finally
         {
            Array.Clear(credentials);
         }
      }

      public AlertKindList BuildAlertsToNotify()
      {
         List<string> kinds = [];

         if (NotifyActivityReview)
         {
            kinds.Add(AlertKinds.ActivityReview);
         }

         if (NotifyDuplicatedPasswords)
         {
            kinds.Add(AlertKinds.DuplicatedPasswords);
         }

         if (NotifyPasswordUpdateReminder)
         {
            kinds.Add(AlertKinds.PasswordUpdateReminder);
         }

         if (NotifyPasswordLeaked)
         {
            kinds.Add(AlertKinds.PasswordLeaked);
         }

         if (NotifySecuritySettings)
         {
            kinds.Add(AlertKinds.VaultSecuritySettings);
            kinds.Add(AlertKinds.HostSecuritySettings);
         }

         if (NotifyInsufficientPasskeys)
         {
            kinds.Add(AlertKinds.InsufficientPasskeys);
         }

         if (NotifyWeakPasskey)
         {
            kinds.Add(AlertKinds.WeakPasskey);
         }

         if (NotifyPasskeyLeaked)
         {
            kinds.Add(AlertKinds.PasskeyLeaked);
         }

         if (NotifyWeakAccountPassword)
         {
            kinds.Add(AlertKinds.WeakAccountPassword);
         }

         if (NotifyPasskeyReusedAsAccountPassword)
         {
            kinds.Add(AlertKinds.PasskeyReusedAsAccountPassword);
         }

         return new AlertKindList(kinds);
      }

      public async Task SaveAsync()
      {
         if (IsBusy)
         {
            return;
         }

         IsBusy = true;
         string[] passkeys = [];
         try
         {
            string username = CredentialRules.NormalizeUsername(Username);
            string error = username.Length == 0 ? Strings.Msg_UsernameEmpty : Passkeys.ValidationError();
            if (error.Length != 0)
            {
               await _dialogs.ErrorAsync(error, Strings.Title_Error).ConfigureAwait(true);
               return;
            }

            Username = username;
            passkeys = Passkeys.RevealAll();

            if (AppServices.Session.Database is { User: not null } database)
            {
               await _saveExistingAsync(database, username, passkeys).ConfigureAwait(true);
            }
            else
            {
               await _createAsync(username, passkeys).ConfigureAwait(true);
            }
         }
         finally
         {
            Array.Clear(passkeys);
            IsBusy = false;
         }
      }

      public async Task DeleteAsync()
      {
         if (IsBusy || AppServices.Session.Database?.User is null)
         {
            return;
         }

         IsBusy = true;
         try
         {
            if (await _dialogs.ConfirmAsync(Strings.Msg_DeleteUserConfirm1, Strings.Title_ConfirmationRequired).ConfigureAwait(true) != DialogResult.Yes
               || await _dialogs.ConfirmAsync(Strings.Msg_DeleteUserConfirm2, Strings.Title_ConfirmationRequired, DialogButtons.YesNoCancel, DialogIcon.Warning).ConfigureAwait(true) != DialogResult.Yes
               || !await ConfirmStoredCredentialsAsync().ConfigureAwait(true))
            {
               return;
            }

            string username = Username;
            Passkeys.ClearSecrets();
            CloseRequested?.Invoke(this, EventArgs.Empty);

            bool deleted;
            try
            {
               deleted = await AppServices.Session.DeleteVaultAsync().ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
               AppServices.Log.Error($"Vault delete failed: {ErrorText.ForLog(ex)}");
               deleted = false;
            }

            if (deleted)
            {
               await _dialogs.InfoAsync(Strings.Format(nameof(Strings.Msg_UserDeleted), username), Strings.Title_Success).ConfigureAwait(true);
            }
            else
            {
               await _dialogs.ErrorAsync(Strings.Login_VaultDeleteRefused, Strings.Title_Error).ConfigureAwait(true);
            }
         }
         finally
         {
            IsBusy = false;
         }
      }

      public async Task ImportAsync()
      {
         if (IsBusy || AppServices.Session.Database is not { User: not null } database)
         {
            return;
         }

         IsBusy = true;
         try
         {
            if (!await _savePendingChangesAsync(database, Strings.Msg_ImportData).ConfigureAwait(true))
            {
               return;
            }

            ExchangeFile? file = await _files.OpenReadAsync(Strings.Title_ImportData, _importExtensions).ConfigureAwait(true);
            if (file is null)
            {
               return;
            }

            ImportExportError imported;
            await using (file.ConfigureAwait(true))
            {
               imported = _importExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)
                  ? await database.ImportFromStreamAsync(file.Stream, file.Extension).ConfigureAwait(true)
                  : ImportExportError.ExtensionFileNotSupported;
            }

            if (imported == ImportExportError.None)
            {
               await _dialogs.InfoAsync(Strings.Msg_ImportSuccess, Strings.Title_ImportSuccess).ConfigureAwait(true);
            }
            else
            {
               string reason = EnumDisplayHelper.FormatFieldValue(nameof(ImportExportError), imported.ToString());
               await _dialogs.ErrorAsync($"{Strings.Msg_ImportFailed}\n{reason}", Strings.Title_ImportFailed).ConfigureAwait(true);
            }
         }
         finally
         {
            IsBusy = false;
         }
      }

      public async Task ExportAsync(bool json)
      {
         if (IsBusy || AppServices.Session.Database is not { User: not null } database)
         {
            return;
         }

         IsBusy = true;
         try
         {
            if (!await _savePendingChangesAsync(database, Strings.Msg_ExportData).ConfigureAwait(true)
               || await _dialogs.ConfirmAsync(Strings.Msg_ExportPlaintextWarning, Strings.Title_ExportPlaintextWarning, DialogButtons.YesNo, DialogIcon.Warning).ConfigureAwait(true) != DialogResult.Yes)
            {
               return;
            }

            string extension = json ? ".json" : ".csv";
            ExchangeFile? file = await _files.OpenWriteAsync(
               json ? Strings.Title_ExportJson : Strings.Title_ExportCsv,
               $"{database.User.ItemId}-{DateTime.Now:yyyyMMddHHmm}{extension}",
               extension).ConfigureAwait(true);
            if (file is null)
            {
               return;
            }

            ImportExportError exported;
            await using (file.ConfigureAwait(true))
            {
               exported = await database.ExportToStreamAsync(file.Stream, extension).ConfigureAwait(true);
            }

            if (exported == ImportExportError.None)
            {
               await _dialogs.InfoAsync(Strings.Msg_ExportSuccess, Strings.Title_ExportSuccess).ConfigureAwait(true);
            }
            else
            {
               string reason = EnumDisplayHelper.FormatFieldValue(nameof(ImportExportError), exported.ToString());
               await _dialogs.ErrorAsync($"{Strings.Msg_ExportFailed}\n{reason}", Strings.Title_ExportFailed).ConfigureAwait(true);
            }
         }
         finally
         {
            IsBusy = false;
         }
      }

      public void OnLanguageChanged()
      {
         string languageCode = SelectedLanguage.Code;
         string themeCode = SelectedTheme.Code;
         Title = _buildTitle();
         Languages = _buildLanguages();
         Themes = _buildThemes();
         SelectedLanguage = _languageFromSettings(languageCode);
         SelectedTheme = _themeFromSettings(themeCode);
      }

      public void OnThemeChanged() => Passkeys.OnThemeChanged();

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         Passkeys.Dispose();
         CloseRequested = null;
      }

      private async Task _createAsync(string username, string[] passkeys)
      {
         if (AppServices.VaultSources is not { } factory
            || AppServices.Paths is not { } paths)
         {
            return;
         }

         if (!await _dialogs.ShowAsync(new ConfirmCredentialsRequest([username, .. passkeys], IsNew: true)).ConfigureAwait(true))
         {
            return;
         }

         string fileName = AppServices.Cryptography.GetHash(username) + ".pku";
         IVaultSource? source;

         if (AppServices.Capabilities.SupportsFileSystemPaths)
         {
            string path = Path.GetFullPath(Path.Join(paths.DefaultDatabaseDirectory, fileName));
            if (await _dialogs.ConfirmAsync(Strings.Format(nameof(Strings.Msg_UseDefaultLocation), path), Strings.Title_UseDefaultLocation).ConfigureAwait(true) != DialogResult.Yes)
            {
               string? picked = await _dialogs.PickSaveFileAsync(Strings.Filter_Pku, Strings.Title_NewUserDatabase, fileName, Path.GetDirectoryName(path)).ConfigureAwait(true);
               if (string.IsNullOrEmpty(picked))
               {
                  return;
               }

               path = picked;
            }

            if (File.Exists(path))
            {
               await _dialogs.ErrorAsync(Strings.Format(nameof(Strings.Msg_VaultTargetExists), Path.GetFileName(path)), Strings.Title_Error).ConfigureAwait(true);
               return;
            }

            source = await factory.CreatePathAsync(path).ConfigureAwait(true);
         }
         else
         {
            source = await factory.CreateInteractiveAsync(fileName).ConfigureAwait(true);
            if (source is null)
            {
               return;
            }
         }

         IDatabase database;
         try
         {
            database = await Core.Models.Database.CreateAsync(
               AppServices.Cryptography,
               AppServices.Serialization,
               AppServices.PasswordFactory,
               AppServices.Clipboard ?? throw new InvalidOperationException("No clipboard service registered."),
               AppServices.SecretProtector,
               source.WorkingPath,
               username,
               passkeys).ConfigureAwait(true);
         }
         catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
         {
            AppServices.Log.Error($"Vault creation failed: {ErrorText.ForLog(ex)}");
            await source.DisposeAsync().ConfigureAwait(true);
            await _dialogs.ErrorAsync(Strings.Format(nameof(Strings.Msg_VaultCreateFailed), Environment.NewLine, ErrorText.Describe(ex)), Strings.Title_Error).ConfigureAwait(true);
            return;
         }

         AppServices.Session.TrackVaultSource(database, source);
         AppServices.Session.StartSession(database);

         if (!await _applyAndSaveAsync(database, username, passkeys).ConfigureAwait(true))
         {
            return;
         }

         Passkeys.ClearSecrets();
         await _dialogs.InfoAsync(Strings.Format(nameof(Strings.Msg_UserCreated), username), Strings.Title_Success).ConfigureAwait(true);
         CloseRequested?.Invoke(this, EventArgs.Empty);
         AppServices.Session.EndSession();
      }

      private async Task _saveExistingAsync(IDatabase database, string username, string[] passkeys)
      {
         IUser user = database.User!;
         string oldFileName = AppServices.Cryptography.GetHash(user.Username) + ".pku";
         string newFileName = AppServices.Cryptography.GetHash(username) + ".pku";

         string[] oldPasskeys = [.. user.Passkeys];
         bool credentialsChanged;
         try
         {
            credentialsChanged = oldFileName != newFileName || AppServices.Serialization.AreDifferent(oldPasskeys, passkeys);
         }
         finally
         {
            Array.Clear(oldPasskeys);
         }

         // Only a vault still named after the username hash is renamed; any other
         // name was chosen by the user and stays.
         bool rename = credentialsChanged
            && oldFileName != newFileName
            && string.Equals(Path.GetFileName(database.DatabaseFile), oldFileName, StringComparison.OrdinalIgnoreCase);

         if (credentialsChanged
            && (!await ConfirmStoredCredentialsAsync().ConfigureAwait(true)
               || !await _dialogs.ShowAsync(new ConfirmCredentialsRequest([username, .. passkeys], IsNew: true)).ConfigureAwait(true)))
         {
            return;
         }

         if (rename
            && AppServices.Capabilities.SupportsFileSystemPaths
            && Path.GetDirectoryName(database.DatabaseFile) is { } directory
            && File.Exists(Path.Join(directory, newFileName)))
         {
            await _dialogs.ErrorAsync(Strings.Format(nameof(Strings.Msg_VaultTargetExists), newFileName), Strings.Title_Error).ConfigureAwait(true);
            return;
         }

         if (!await _applyAndSaveAsync(database, username, passkeys).ConfigureAwait(true))
         {
            return;
         }

         if (!credentialsChanged)
         {
            await _dialogs.InfoAsync(Strings.Format(nameof(Strings.Msg_UserUpdated), username), Strings.Title_Success).ConfigureAwait(true);
            CloseRequested?.Invoke(this, EventArgs.Empty);
            return;
         }

         Passkeys.ClearSecrets();
         await _dialogs.InfoAsync(Strings.Format(nameof(Strings.Msg_CredentialsUpdated), username), Strings.Title_Success).ConfigureAwait(true);
         CloseRequested?.Invoke(this, EventArgs.Empty);

         if (!rename)
         {
            AppServices.Session.EndSession();
            return;
         }

         VaultRenameOutcome outcome = await AppServices.Session.EndSessionAndRenameAsync(newFileName).ConfigureAwait(true);
         if (!outcome.Renamed)
         {
            await _dialogs.WarnAsync(
               Strings.Format(nameof(Strings.Msg_VaultRenameKept), Environment.NewLine, outcome.Location ?? oldFileName),
               Strings.Title_Error).ConfigureAwait(true);
         }
      }

      /// <summary>Applies the edited values and saves; restores the previous values when saving fails.</summary>
      private async Task<bool> _applyAndSaveAsync(IDatabase database, string username, string[] passkeys)
      {
         IUser user = database.User!;
         ISettings settings = user.Settings;
         AlertKindList alertsToNotify = BuildAlertsToNotify();

         if (alertsToNotify.Count == 0)
         {
            await _dialogs.WarnAsync(Strings.Msg_NoAlertsToNotify, Strings.Title_NoAlertsToNotify).ConfigureAwait(true);
         }

         string oldUsername = user.Username;
         string[] oldPasskeys = [.. user.Passkeys];
         (int, int, int, int, int, string, string, AlertKindList) oldSettings = (
            settings.LogoutTimeout,
            settings.CleaningClipboardTimeout,
            settings.ShowPasswordDelay,
            settings.NumberOfOldPasswordToKeep,
            settings.NumberOfMonthActivitiesToKeep,
            settings.Language,
            settings.Theme,
            settings.AlertsToNotify);

         try
         {
            user.Username = username;
            user.Passkeys = passkeys;
            settings.LogoutTimeout = LogoutTimeout;
            settings.CleaningClipboardTimeout = CleaningClipboardTimeout;
            settings.ShowPasswordDelay = ShowPasswordDelay;
            settings.NumberOfOldPasswordToKeep = NumberOfOldPasswordToKeep;
            settings.NumberOfMonthActivitiesToKeep = NumberOfMonthActivitiesToKeep;
            settings.Language = SelectedLanguage.Code;
            settings.Theme = SelectedTheme.Code;
            settings.AlertsToNotify = alertsToNotify;

            await database.SaveAsync().ConfigureAwait(true);
         }
         catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
         {
            AppServices.Log.Error($"User settings save failed: {ErrorText.ForLog(ex)}");
            user.Username = oldUsername;
            user.Passkeys = oldPasskeys;
            (settings.LogoutTimeout,
               settings.CleaningClipboardTimeout,
               settings.ShowPasswordDelay,
               settings.NumberOfOldPasswordToKeep,
               settings.NumberOfMonthActivitiesToKeep,
               settings.Language,
               settings.Theme,
               settings.AlertsToNotify) = oldSettings;

            await _dialogs.ErrorAsync(Strings.Format(nameof(Strings.Msg_UserSaveFailed), Environment.NewLine, ErrorText.Describe(ex)), Strings.Title_Error).ConfigureAwait(true);
            return false;
         }
         finally
         {
            Array.Clear(oldPasskeys);
         }

         AppServices.Session.ApplySessionLanguage();
         AppServices.Session.ApplySessionTheme();
         return true;
      }

      private static async Task<bool> _savePendingChangesAsync(IDatabase database, string title)
      {
         if (!database.User!.HasChanged())
         {
            return true;
         }

         if (await _dialogs.ConfirmAsync(Strings.Msg_SaveBeforeContinue, title, DialogButtons.OKCancel, DialogIcon.None).ConfigureAwait(true) != DialogResult.OK)
         {
            return false;
         }

         await database.SaveAsync().ConfigureAwait(true);
         return true;
      }

      private static Task _viewActivitiesAsync()
         => AppServices.Session.User is { } user
            ? _dialogs.ShowAsync(new ActivitiesRequest(NeedsReview: false, SearchCriteria: user.ItemId))
            : Task.CompletedTask;

      private static string _buildTitle()
         => AppServices.Session.Database?.User is null
            ? Strings.Format(nameof(Strings.Title_NewUser), AppServices.Info.Title)
            : Strings.Format(nameof(Strings.Title_UserSettings), AppServices.Info.Title);

      private static IReadOnlyList<AppLanguage> _buildLanguages()
         =>
         [
            new(string.Empty, Strings.Label_UseAppLanguage),
            .. LocalizationService.Supported,
         ];

      private static IReadOnlyList<AppThemeOption> _buildThemes()
         =>
         [
            new(string.Empty, Strings.Label_UseAppTheme),
            .. ThemePreferences.Options,
         ];

      private AppLanguage _languageFromSettings(string? code)
         => string.IsNullOrWhiteSpace(code)
            ? Languages[0]
            : Languages.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase)) ?? Languages[0];

      private AppThemeOption _themeFromSettings(string? code)
         => string.IsNullOrWhiteSpace(code)
            ? Themes[0]
            : Themes.FirstOrDefault(t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase)) ?? Themes[0];
   }
}
