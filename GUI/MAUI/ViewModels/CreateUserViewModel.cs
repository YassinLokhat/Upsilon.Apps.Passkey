using System.Collections.ObjectModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class PasskeyEntry : ObservableObject
   {
      public string Value
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;
   }

   internal sealed class CreateUserViewModel : ObservableObject
   {
      private readonly AsyncRelayCommand _createCommand;

      public CreateUserViewModel()
      {
         Passkeys.Add(new PasskeyEntry());
         _createCommand = new AsyncRelayCommand(_createAsync, () => !IsBusy);
         AddPasskeyCommand = new RelayCommand(() => Passkeys.Add(new PasskeyEntry()));
         RemovePasskeyCommand = new RelayCommand(p =>
         {
            if (p is PasskeyEntry entry && Passkeys.Count > 1)
            {
               _ = Passkeys.Remove(entry);
            }
         });
      }

      public string Username
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public ObservableCollection<PasskeyEntry> Passkeys { get; } = [];

      public bool IsBusy
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _createCommand.NotifyCanExecuteChanged();
            }
         }
      }

      public string BusyMessage
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public ICommand CreateCommand => _createCommand;
      public ICommand AddPasskeyCommand { get; }
      public ICommand RemovePasskeyCommand { get; }

      private async Task _createAsync()
      {
         string username = Username.Trim();
         if (string.IsNullOrEmpty(username))
         {
            await AppServices.Dialogs.WarnAsync(Strings.Msg_EnterUsername, Strings.Title_Error).ConfigureAwait(true);
            return;
         }

         List<string> passkeys = Passkeys
            .Select(p => p.Value)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

         if (passkeys.Count == 0)
         {
            await AppServices.Dialogs.WarnAsync(Strings.Msg_NeedPasskey, Strings.Title_Error).ConfigureAwait(true);
            return;
         }

         string hash = AppServices.Cryptography.GetHash(username);
         string defaultPath = Path.GetFullPath(Path.Join(
            PasskeyAppInfo.AppSettings.DefaultDatabaseDirectory,
            $"{hash}.pku"));

         bool useDefault = await AppServices.Dialogs.ConfirmAsync(
            Strings.Format(nameof(Strings.Msg_UseDefaultLocation), defaultPath),
            Strings.Title_UseDefaultLocation).ConfigureAwait(true);

         string vaultPath = defaultPath;
         if (!useDefault)
         {
            string? picked = await AppServices.Dialogs.PickSaveFileAsync(
               Strings.Title_NewUser,
               defaultPath).ConfigureAwait(true);
            if (string.IsNullOrEmpty(picked))
            {
               return;
            }

            vaultPath = picked;
         }

         IsBusy = true;
         BusyMessage = Strings.Msg_CreatingDatabase;

         try
         {
            string? dir = Path.GetDirectoryName(vaultPath);
            if (!string.IsNullOrEmpty(dir))
            {
               _ = Directory.CreateDirectory(dir);
            }

            IDatabase database = await Core.Models.Database.CreateAsync(
               AppServices.Cryptography,
               AppServices.Serialization,
               AppServices.PasswordFactory,
               AppServices.Clipboard,
               vaultPath,
               username,
               passkeys).ConfigureAwait(true);

            AppServices.Session.StartSession(database);
            AppServices.Session.ApplySessionLanguage();
            AppServices.Session.ApplySessionTheme();

            await AppServices.Navigation.GoToServicesAsync().ConfigureAwait(true);
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or ArgumentNullException
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException
            or DirectoryNotFoundException
            or NotSupportedException
            or InsufficientKdfParametersException)
         {
            Log.Error(ex, "Failed to create vault");
            await AppServices.Dialogs.WarnAsync(ex.Message, Strings.Title_Error).ConfigureAwait(true);
         }
         finally
         {
            IsBusy = false;
            BusyMessage = string.Empty;
         }
      }
   }
}
