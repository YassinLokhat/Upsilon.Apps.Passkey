using System.IO;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;
using Upsilon.Apps.Passkey.GUI.MAUI.Themes;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class LoginViewModel : ObservableObject
   {
      private readonly AsyncRelayCommand _submitCommand;
      private readonly AsyncRelayCommand _openVaultCommand;
      private readonly AsyncRelayCommand _createUserCommand;
      private readonly RelayCommand _cancelCommand;

      public LoginViewModel()
      {
         _submitCommand = new AsyncRelayCommand(_submitAsync, () => !IsBusy);
         _openVaultCommand = new AsyncRelayCommand(_openVaultAsync, () => !IsBusy && !IsAwaitingPasskeys);
         _createUserCommand = new AsyncRelayCommand(_createUserAsync, () => !IsBusy && !IsAwaitingPasskeys);
         _cancelCommand = new RelayCommand(_cancel, () => IsAwaitingPasskeys || !string.IsNullOrEmpty(Username));
         ToggleLanguageCommand = new RelayCommand(_toggleLanguage);
         ToggleThemeCommand = new RelayCommand(_toggleTheme);
      }

      public string AppTitle => PasskeyAppInfo.Title;

      public string DatabaseFile
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(DatabaseLabel));
            }
         }
      } = string.Empty;

      public string DatabaseLabel => File.Exists(DatabaseFile)
         ? Strings.Format(nameof(Strings.Msg_DatabaseLabel), Path.GetFileName(DatabaseFile))
         : Strings.Msg_NoDatabaseLoaded;

      public string CredentialsLabel => IsAwaitingPasskeys ? Strings.Label_Password : Strings.Label_Username;

      public string CredentialsHint => IsAwaitingPasskeys ? Strings.Msg_EnterPasskey : Strings.Msg_EnterUsername;

      public string Username
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string Passkey
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public bool IsBusy
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _notifyCommands();
            }
         }
      }

      public string BusyMessage
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public bool IsAwaitingPasskeys
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(CredentialsLabel));
               OnPropertyChanged(nameof(CredentialsHint));
               OnPropertyChanged(nameof(ShowUsername));
               OnPropertyChanged(nameof(ShowPasskey));
               _notifyCommands();
            }
         }
      }

      public bool ShowUsername => !IsAwaitingPasskeys;

      public bool ShowPasskey => IsAwaitingPasskeys;

      public ICommand SubmitCommand => _submitCommand;
      public ICommand OpenVaultCommand => _openVaultCommand;
      public ICommand CreateUserCommand => _createUserCommand;
      public ICommand CancelCommand => _cancelCommand;
      public ICommand ToggleLanguageCommand { get; }
      public ICommand ToggleThemeCommand { get; }

      public void RefreshLabels()
      {
         OnPropertyChanged(nameof(DatabaseLabel));
         OnPropertyChanged(nameof(CredentialsLabel));
         OnPropertyChanged(nameof(CredentialsHint));
         OnPropertyChanged(nameof(AppTitle));
      }

      private void _toggleLanguage()
      {
         string next = string.Equals(PasskeyAppInfo.AppSettings.Language, "fr", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "fr";
         PasskeyAppInfo.AppSettings.Language = next;
         PasskeyAppInfo.AppSettings.Save(PasskeyAppInfo.ConfigFile);
         _ = LocalizationService.Apply(next, forceRefresh: true);
         RefreshLabels();
      }

      private void _toggleTheme()
      {
         string current = PasskeyAppInfo.AppSettings.Theme;
         string next = string.Equals(current, ThemeService.DarkCode, StringComparison.OrdinalIgnoreCase)
            ? ThemeService.LightCode
            : ThemeService.DarkCode;
         PasskeyAppInfo.AppSettings.Theme = next;
         PasskeyAppInfo.AppSettings.Save(PasskeyAppInfo.ConfigFile);
         _ = ThemeService.Apply(next, forceRefresh: true);
      }

      private async Task _submitAsync()
      {
         if (IsAwaitingPasskeys)
         {
            await _submitPasskeyAsync().ConfigureAwait(true);
         }
         else
         {
            await _submitUsernameAsync().ConfigureAwait(true);
         }
      }

      private async Task _submitUsernameAsync()
      {
         if (string.IsNullOrWhiteSpace(Username))
         {
            return;
         }

         if (!File.Exists(DatabaseFile))
         {
            string filename = AppServices.Cryptography.GetHash(Username.Trim());
            DatabaseFile = AppPaths.VaultPathForUsername(filename);
         }

         IsBusy = true;
         BusyMessage = Strings.Msg_OpeningDatabase;

         try
         {
            IDatabase database = await Database.OpenAsync(
               AppServices.Cryptography,
               AppServices.Serialization,
               AppServices.PasswordFactory,
               AppServices.Clipboard,
               DatabaseFile,
               Username.Trim()).ConfigureAwait(true);

            database.DatabaseClosed += _onDatabaseClosed;
            database.AutoSaveDetected += _onAutoSaveDetected;
            AppServices.Session.StartSession(database);
         }
         catch (InsufficientKdfParametersException ex)
         {
            Log.Error(ex, "Failed to open database");
            await AppServices.Dialogs.WarnAsync(Strings.Msg_InsufficientKdf, Strings.Title_InsufficientKdf)
               .ConfigureAwait(true);
            return;
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or ArgumentNullException
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException
            or FileNotFoundException
            or DirectoryNotFoundException
            or NotSupportedException
            or CorruptedSourceException
            or WrongPasswordException)
         {
            Log.Error(ex, "Failed to open database");
            await AppServices.Dialogs.WarnAsync(ex.Message, Strings.Title_Error).ConfigureAwait(true);
            return;
         }
         finally
         {
            IsBusy = false;
            BusyMessage = string.Empty;
         }

         if (AppServices.Session.Database is null)
         {
            return;
         }

         IsAwaitingPasskeys = true;
         Username = string.Empty;
         Passkey = string.Empty;
      }

      private async Task _submitPasskeyAsync()
      {
         IDatabase? database = AppServices.Session.Database;
         if (database is null || string.IsNullOrEmpty(Passkey))
         {
            return;
         }

         string passkey = Passkey;
         Passkey = string.Empty;

         IsBusy = true;
         BusyMessage = Strings.Msg_CheckingPasskey;

         try
         {
            _ = await database.LoginAsync(passkey).ConfigureAwait(true);
         }
         catch (CorruptedSourceException ex)
         {
            Log.Error(ex, "Database corrupted during login");
            await AppServices.Dialogs.WarnAsync(Strings.Msg_CorruptedDatabase, Strings.Title_CorruptedDatabase)
               .ConfigureAwait(true);
            _resetCredentials();
            AppServices.Session.EndSession();
            return;
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or ArgumentNullException
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or WrongPasswordException)
         {
            Log.Error(ex, "Login failed");
            await AppServices.Dialogs.WarnAsync(ex.Message, Strings.Title_Error).ConfigureAwait(true);
            _resetCredentials();
            AppServices.Session.EndSession();
            return;
         }
         finally
         {
            IsBusy = false;
            BusyMessage = string.Empty;
         }

         if (AppServices.Session.User is null)
         {
            // Incomplete onion or wrong passkey — stay on passkey prompt.
            return;
         }

         AppServices.Session.ApplySessionLanguage();
         AppServices.Session.ApplySessionTheme();
         _resetCredentials();
         await AppServices.Navigation.GoToServicesAsync().ConfigureAwait(true);
      }

      private async Task _openVaultAsync()
      {
         string? path = await AppServices.Dialogs.PickOpenFileAsync(Strings.Title_OpenDatabase).ConfigureAwait(true);
         if (path is null)
         {
            return;
         }

         _resetCredentials();
         AppServices.Session.EndSession();
         DatabaseFile = path;
      }

      private async Task _createUserAsync()
      {
         await Shell.Current.GoToAsync(nameof(Views.CreateUserPage)).ConfigureAwait(true);
      }

      private void _cancel()
      {
         _resetCredentials();
         AppServices.Session.EndSession();
      }

      private void _resetCredentials()
      {
         IsAwaitingPasskeys = false;
         Username = string.Empty;
         Passkey = string.Empty;
      }

      private void _onDatabaseClosed(object? sender, LogoutEventArgs e)
      {
         if (sender is IDatabase database)
         {
            database.AutoSaveDetected -= _onAutoSaveDetected;
         }

         MainThread.BeginInvokeOnMainThread(() =>
         {
            AppServices.Session.EndSession(closeDatabase: false);
            _resetCredentials();
            _ = AppServices.Navigation.GoToLoginAsync();
         });
      }

      private void _onAutoSaveDetected(object? sender, AutoSaveDetectedEventArgs e)
      {
         ArgumentNullException.ThrowIfNull(e);

         // LoginAsync raises this from a worker thread and blocks on MergeBehavior;
         // marshal to the UI thread like WPF MainWindow.
         AutoSaveMergeBehavior behavior = MainThread.InvokeOnMainThreadAsync(async () =>
         {
            ConfirmThreeWayResult result = await AppServices.Dialogs
               .ConfirmThreeWayAsync(Strings.Msg_AutosaveDetected, Strings.Title_AutosaveDetected)
               .ConfigureAwait(true);

            return result switch
            {
               ConfirmThreeWayResult.Cancel => AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile,
               ConfirmThreeWayResult.No => AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile,
               _ => AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile,
            };
         }).GetAwaiter().GetResult();

         e.MergeBehavior = behavior;
      }

      private void _notifyCommands()
      {
         _submitCommand.NotifyCanExecuteChanged();
         _openVaultCommand.NotifyCanExecuteChanged();
         _createUserCommand.NotifyCanExecuteChanged();
         _cancelCommand.NotifyCanExecuteChanged();
      }
   }
}
