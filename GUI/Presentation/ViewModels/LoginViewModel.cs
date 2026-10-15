using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels
{
   /// <summary>
   /// Progressive login without binding passkeys to the ViewModel.
   /// Hosts read the secret from the control and pass it to <see cref="SubmitPasskeyAsync"/>.
   /// </summary>
   internal sealed class LoginViewModel : ObservableObject
   {
      private IDatabase? _database;
      private IUser? _user;
      private IVaultSource? _vaultSource;
      private bool _isBusy;
      private string _username = string.Empty;
      private string _statusMessage = string.Empty;

      public string Username
      {
         get => _username;
         set => _ = SetProperty(ref _username, value);
      }

      public bool DatabaseOpened => _database is not null;

      public bool IsLoggedIn => _user is not null;

      public bool IsBusy
      {
         get => _isBusy;
         private set => _ = SetProperty(ref _isBusy, value);
      }

      public string StatusMessage
      {
         get => _statusMessage;
         private set => _ = SetProperty(ref _statusMessage, value);
      }

      public string CredentialsLabel
         => DatabaseOpened ? "Passkey :" : "Username :";

      public IDatabase? Database => _database;

      public IUser? User => _user;

      public async Task OpenByUsernameAsync(CancellationToken cancellationToken = default)
      {
         if (AppServices.Paths is null
             || AppServices.VaultSources is null
             || AppServices.Clipboard is null)
         {
            StatusMessage = "Host paths are not configured.";
            return;
         }

         string hash = AppServices.Cryptography.GetHash(Username.Trim());
         string path = Path.Join(AppServices.Paths.DefaultDatabaseDirectory, hash + ".pku");
         if (!File.Exists(path))
         {
            StatusMessage = "Vault not found.";
            return;
         }

         await OpenPathAsync(path, cancellationToken).ConfigureAwait(false);
      }

      public async Task OpenPathAsync(string path, CancellationToken cancellationToken = default)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(path);
         if (AppServices.VaultSources is null
             || AppServices.Clipboard is null)
         {
            StatusMessage = "Host paths are not configured.";
            return;
         }

         IsBusy = true;
         StatusMessage = "Opening…";
         try
         {
            _resetSessionCore();
            _vaultSource = await AppServices.VaultSources.OpenPathAsync(path, cancellationToken).ConfigureAwait(false);
            await _vaultSource.CheckoutAsync(cancellationToken).ConfigureAwait(false);

            _database = await Core.Models.Database.OpenAsync(
               AppServices.Cryptography,
               AppServices.Serialization,
               AppServices.PasswordFactory,
               AppServices.Clipboard,
               AppServices.SecretProtector,
               _vaultSource.WorkingPath,
               Username.Trim(),
               cancellationToken).ConfigureAwait(false);

            OnPropertyChanged(nameof(DatabaseOpened));
            OnPropertyChanged(nameof(CredentialsLabel));
            StatusMessage = "Enter passkeys in order.";
         }
         catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or InvalidOperationException
            or CorruptedSourceException
            or InsufficientKdfParametersException
            or NullValueException)
         {
            StatusMessage = $"Open failed: {ex.GetType().Name}";
            _resetSessionCore();
         }
         finally
         {
            IsBusy = false;
         }
      }

      /// <summary>Pass the passkey from the control; do not store it on this ViewModel.</summary>
      public async Task<bool> SubmitPasskeyAsync(string passkey, CancellationToken cancellationToken = default)
      {
         if (_database is null
             || string.IsNullOrEmpty(passkey))
         {
            return false;
         }

         IsBusy = true;
         try
         {
            _user = await _database.LoginAsync(passkey, cancellationToken).ConfigureAwait(false);
            OnPropertyChanged(nameof(IsLoggedIn));
            if (_user is not null)
            {
               StatusMessage = "Logged in.";
               return true;
            }

            StatusMessage = "Passkey accepted (more required) or session poisoned — Escape to reset.";
            return false;
         }
         finally
         {
            IsBusy = false;
         }
      }

      public void Reset()
      {
         _resetSessionCore();
         StatusMessage = string.Empty;
         OnPropertyChanged(nameof(DatabaseOpened));
         OnPropertyChanged(nameof(CredentialsLabel));
         OnPropertyChanged(nameof(IsLoggedIn));
      }

      private void _resetSessionCore()
      {
         _database?.Close();
         _database = null;
         _user = null;
         _vaultSource = null;
      }
   }
}
