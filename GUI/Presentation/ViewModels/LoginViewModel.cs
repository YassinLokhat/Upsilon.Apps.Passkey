using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
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
   internal sealed class LoginViewModel : ObservableObject
   {
      private IDatabase? _database;
      private IUser? _user;
      private bool _isBusy;
      private string? _pickedVault;
      private string _pickedVaultName = string.Empty;
      private string _statusMessage = string.Empty;

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
            }
         }
      }

      public bool HasSelectedVault => _pickedVaultName.Length != 0;

      public string CredentialsLabel
         => DatabaseOpened ? "Passkey :" : "Username :";

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
         StatusMessage = "Enter the username of this vault.";
      }

      public async Task SubmitUsernameAsync(string username, CancellationToken cancellationToken = default)
      {
         username = username.Trim();
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
            StatusMessage = "Host paths are not configured.";
            return;
         }

         string location = _pickedVault
            ?? Path.GetFullPath(Path.Join(AppServices.Paths.DefaultDatabaseDirectory, AppServices.Cryptography.GetHash(username) + ".pku"));

         if (_pickedVault is null
             && !File.Exists(location))
         {
            StatusMessage = $"No vault for this username in '{AppServices.Paths.DefaultDatabaseDirectory}'. Use \"Open vault…\" to pick the file.";
            return;
         }

         IsBusy = true;
         StatusMessage = "Opening…";
         IDatabase? database = null;
         try
         {
            IVaultSource source = await AppServices.VaultSources.OpenPathAsync(location, cancellationToken).ConfigureAwait(true);
            await source.CheckoutAsync(cancellationToken).ConfigureAwait(true);

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
            database.DatabaseClosed += (sender, e) => _onDatabaseClosed(database, source, e);
            _database = database;

            OnPropertyChanged(nameof(DatabaseOpened));
            OnPropertyChanged(nameof(CredentialsLabel));
            StatusMessage = "Enter passkeys in order. Reset to start over after a typo.";
         }
         catch (InsufficientKdfParametersException)
         {
            StatusMessage = "This vault uses key-derivation parameters outside the accepted range; it was refused.";
         }
         catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or InvalidOperationException
            or CorruptedSourceException
            or NullValueException)
         {
            StatusMessage = $"Open failed: {ex.Message}";
         }
         finally
         {
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
         StatusMessage = "Checking passkey…";
         try
         {
            _user = await database.LoginAsync(passkey, cancellationToken).ConfigureAwait(true);
         }
         catch (CorruptedSourceException)
         {
            Reset();
            StatusMessage = "The vault is corrupted or is not a Passkey vault.";
            return false;
         }
         catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or NullValueException)
         {
            Reset();
            StatusMessage = $"Login failed: {ex.Message}";
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
            StatusMessage = "Next passkey (Reset to start over).";
            return false;
         }

         StatusMessage = string.Empty;
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

         // Close raises DatabaseClosed, which writes the working copy back.
         database?.Close();
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
               "Unsaved changes from a previous session were found.\nYes: merge and save. No: discard them. Cancel: merge without saving and decide later.",
               "Autosave detected",
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

      private void _onDatabaseClosed(IDatabase database, IVaultSource source, LogoutEventArgs e)
      {
         // May run on the session-timer thread (auto-logout).
         _ = _writeBackAsync(source);

         void endSession()
         {
            if (ReferenceEquals(_database, database))
            {
               _clearSessionState();
            }

            SessionEnded?.Invoke(this, e);
         }

         if (AppServices.Ui is { } ui)
         {
            ui.Post(endSession);
         }
         else
         {
            endSession();
         }
      }

      private async Task _writeBackAsync(IVaultSource source)
      {
         string? message;
         try
         {
            message = await source.WriteBackAsync().ConfigureAwait(false) switch
            {
               VaultWriteBackResult.Conflict => "The vault source changed elsewhere; your copy was kept locally and the source was not overwritten.",
               VaultWriteBackResult.Failed => "Could not write the vault back to its source.",
               _ => null,
            };
         }
         catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or InvalidOperationException)
         {
            message = $"Could not write the vault back to its source: {ex.Message}";
         }

         if (message is not null)
         {
            AppServices.Ui?.Post(() => StatusMessage = message);
         }
      }
   }
}
