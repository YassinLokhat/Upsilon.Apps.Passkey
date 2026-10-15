using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels
{
   /// <summary>
   /// Progressive login without binding passkeys to the ViewModel. Mirrors the
   /// WPF login: an explicitly picked vault wins over the default directory,
   /// then username, then each passkey in order (no rollback).
   /// Hosts read the secret from the control and pass it to <see cref="SubmitPasskeyAsync"/>.
   /// Must be driven from the UI thread.
   /// </summary>
   internal sealed class LoginViewModel : ObservableObject, IDisposable
   {
      private IDatabase? _database;
      private IUser? _user;
      private bool _isBusy;
      private string? _pickedVault;
      private string _pickedVaultName = string.Empty;
      private string _statusMessage = string.Empty;
      private readonly ISessionService _session;
      private bool _disposed;

      public LoginViewModel()
      {
         _session = AppServices.Session;
         _session.WriteBackCompleted += _onWriteBackCompleted;
      }

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         _session.WriteBackCompleted -= _onWriteBackCompleted;
         LoggedIn = null;
         SessionEnded = null;
      }

      /// <summary>Raised on the UI thread once the last passkey unlocked the vault.</summary>
      public event EventHandler? LoggedIn;

      /// <summary>Raised on the UI thread after the session closed (logout, timeout, reset).</summary>
      public event EventHandler<LogoutEventArgs>? SessionEnded;

      public bool DatabaseOpened => _database is not null;

      public bool IsLoggedIn => _user is not null;

      public bool IsBusy
      {
         get => _isBusy;
         private set
         {
            if (SetProperty(ref _isBusy, value))
            {
               OnPropertyChanged(nameof(IsIdle));
            }
         }
      }

      public bool IsIdle => !_isBusy;

      public string StatusMessage
      {
         get => _statusMessage;
         private set => _ = SetProperty(ref _statusMessage, value);
      }

      public string SelectedVaultName
      {
         get => _pickedVaultName;
         private set
         {
            if (SetProperty(ref _pickedVaultName, value))
            {
               OnPropertyChanged(nameof(HasSelectedVault));
               OnPropertyChanged(nameof(SelectedVaultLabel));
            }
         }
      }

      public bool HasSelectedVault => _pickedVaultName.Length != 0;

      public string SelectedVaultLabel
         => HasSelectedVault ? Strings.Format(nameof(Strings.Login_SelectedVault), _pickedVaultName) : string.Empty;

      public string CredentialsLabel
         => DatabaseOpened ? Strings.Label_Password : Strings.Label_Username;

      /// <summary>Re-reads culture-dependent labels after <see cref="LocalizationService.LanguageChanged"/>.</summary>
      public void OnLanguageChanged()
      {
         OnPropertyChanged(nameof(CredentialsLabel));
         OnPropertyChanged(nameof(SelectedVaultLabel));
      }

      public IDatabase? Database => _database;

      public IUser? User => _user;

      /// <summary>
      /// Remembers an explicitly picked vault (path, or SAF URI on Android).
      /// The next username is opened against it instead of the default directory.
      /// </summary>
      public void SelectVault(string location, string displayName)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(location);
         if (IsBusy)
         {
            return;
         }

         Reset();
         _pickedVault = location;
         SelectedVaultName = displayName;
         StatusMessage = Strings.Login_EnterVaultUsername;
      }

      public async Task SubmitUsernameAsync(string username, CancellationToken cancellationToken = default)
      {
         username = CredentialRules.NormalizeUsername(username);
         if (IsBusy
             || DatabaseOpened
             || username.Length == 0)
         {
            return;
         }

         if (AppServices.Paths is null
             || AppServices.VaultSources is null
             || AppServices.Clipboard is null)
         {
            StatusMessage = Strings.Login_HostNotConfigured;
            return;
         }

         string location = _pickedVault
            ?? Path.GetFullPath(Path.Join(AppServices.Paths.DefaultDatabaseDirectory, AppServices.Cryptography.GetHash(username) + ".pku"));

         if (_pickedVault is null
             && !File.Exists(location))
         {
            StatusMessage = Strings.Format(
               nameof(Strings.Login_NoVaultForUsername),
               AppServices.Paths.DefaultDatabaseDirectory,
               Strings.Login_OpenVault);
            return;
         }

         IsBusy = true;
         StatusMessage = Strings.Login_Opening;
         IDatabase? database = null;
         IVaultSource? source = null;
         try
         {
            source = await AppServices.VaultSources.OpenPathAsync(location, cancellationToken).ConfigureAwait(true);
            VaultCheckoutResult checkout = await source.CheckoutAsync(cancellationToken).ConfigureAwait(true);

            database = await Core.Models.Database.OpenAsync(
               AppServices.Cryptography,
               AppServices.Serialization,
               AppServices.PasswordFactory,
               AppServices.Clipboard,
               AppServices.SecretProtector,
               source.WorkingPath,
               username,
               cancellationToken).ConfigureAwait(true);

            database.AutoSaveDetected += _onAutoSaveDetected;
            AppServices.Session.TrackVaultSource(database, source);
            source = null;
            database.DatabaseClosed += _onDatabaseClosed;
            _database = database;

            OnPropertyChanged(nameof(DatabaseOpened));
            OnPropertyChanged(nameof(CredentialsLabel));
            StatusMessage = checkout switch
            {
               VaultCheckoutResult.RecoveredLocalChanges => $"{Strings.Login_RecoveredLocalChanges} {Strings.Login_EnterPasskeys}",
               VaultCheckoutResult.StaleCopyKeptAsConflict => $"{Strings.Login_StaleCopyKeptAsConflict} {Strings.Login_EnterPasskeys}",
               _ => Strings.Login_EnterPasskeys,
            };
         }
         catch (InsufficientKdfParametersException)
         {
            StatusMessage = Strings.Msg_InsufficientKdf;
         }
         catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or InvalidOperationException
            or CorruptedSourceException
            or NullValueException)
         {
            StatusMessage = Strings.Format(nameof(Strings.Login_OpenFailed), ErrorText.Describe(ex));
         }
         finally
         {
            // Not handed to the session (open failed): release it now. An
            // untouched working copy is removed; recovered edits are kept.
            if (source is not null)
            {
               await source.DisposeAsync().ConfigureAwait(true);
            }

            IsBusy = false;
         }
      }

      /// <summary>Pass the passkey from the control; do not store it on this ViewModel.</summary>
      public async Task<bool> SubmitPasskeyAsync(string passkey, CancellationToken cancellationToken = default)
      {
         IDatabase? database = _database;
         if (IsBusy
             || IsLoggedIn
             || database is null
             || string.IsNullOrEmpty(passkey))
         {
            return false;
         }

         IsBusy = true;
         StatusMessage = Strings.Login_CheckingPasskey;
         try
         {
            _user = await database.LoginAsync(passkey, cancellationToken).ConfigureAwait(true);
         }
         catch (CorruptedSourceException)
         {
            Reset();
            StatusMessage = Strings.Msg_CorruptedDatabase;
            return false;
         }
         catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or NullValueException)
         {
            Reset();
            StatusMessage = Strings.Format(nameof(Strings.Login_LoginFailed), ErrorText.Describe(ex));
            return false;
         }
         finally
         {
            IsBusy = false;
         }

         OnPropertyChanged(nameof(IsLoggedIn));

         if (_user is null)
         {
            // Same soft feedback as WPF: a wrong layer is not reported (no rollback).
            StatusMessage = Strings.Login_NextPasskey;
            return false;
         }

         StatusMessage = string.Empty;
         AppServices.Session.StartSession(database);
         LoggedIn?.Invoke(this, EventArgs.Empty);
         return true;
      }

      /// <summary>Closes any open or half-open session and clears the picked vault.</summary>
      public void Reset()
      {
         IDatabase? database = _database;
         _clearSessionState();
         _pickedVault = null;
         SelectedVaultName = string.Empty;

         // Close raises DatabaseClosed; the session then writes the working copy back.
         if (database is not null && ReferenceEquals(AppServices.Session.Database, database))
         {
            AppServices.Session.EndSession();
         }
         else
         {
            database?.Close();
         }
         StatusMessage = string.Empty;
      }

      private void _clearSessionState()
      {
         _database = null;
         _user = null;
         OnPropertyChanged(nameof(DatabaseOpened));
         OnPropertyChanged(nameof(CredentialsLabel));
         OnPropertyChanged(nameof(IsLoggedIn));
      }

      private void _onAutoSaveDetected(object? sender, AutoSaveDetectedEventArgs e)
      {
         // Raised by LoginAsync on a worker thread, which blocks for the answer.
         // The UI thread is awaiting (not blocking on) the login, so a synchronous
         // hop to it is safe — the same contract as the WPF Dispatcher.Invoke.
         IUiDispatcher? ui = AppServices.Ui;
         IDialogService? dialogs = AppServices.Dialogs;
         if (ui is null
             || dialogs is null
             || ui.CheckAccess())
         {
            e.MergeBehavior = AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile;
            return;
         }

         DialogResult answer = ui.Send(() => dialogs.ConfirmAsync(
               Strings.Msg_AutosaveDetected,
               Strings.Title_AutosaveDetected,
               DialogButtons.YesNoCancel))
            .GetAwaiter()
            .GetResult();

         e.MergeBehavior = answer switch
         {
            DialogResult.Yes => AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile,
            DialogResult.No => AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile,
            _ => AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile,
         };
      }

      private void _onDatabaseClosed(object? sender, LogoutEventArgs e)
      {
         // May run on the session-timer thread (auto-logout).
         if (sender is not IDatabase database)
         {
            return;
         }

         database.DatabaseClosed -= _onDatabaseClosed;
         database.AutoSaveDetected -= _onAutoSaveDetected;

         UiThread.Post(() =>
         {
            if (ReferenceEquals(AppServices.Session.Database, database))
            {
               AppServices.Session.EndSession(closeDatabase: false);
            }

            if (ReferenceEquals(_database, database))
            {
               _clearSessionState();
            }

            SessionEnded?.Invoke(this, e);
         });
      }

      private void _onWriteBackCompleted(object? sender, VaultWriteBackEventArgs e)
         => StatusMessage = e.Message;
   }
}
