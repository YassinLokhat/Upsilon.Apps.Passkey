using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class MainPageViewModel : INotifyPropertyChanged
   {
      public static string AppTitle => Helper.AppInfo.Title;

      public string WindowTitle
      {
         get;
         set
         {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowTitle)));
         }
      } = AppTitle;

      public string CredentialsLabel => !DatabaseOpened ? "Username :" : "Password :";

      public string ActualCredential
      {
         get;
         set
         {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActualCredential)));
         }
      } = string.Empty;

      public bool DatabaseOpened
      {
         get;
         set
         {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DatabaseOpened)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CredentialsLabel)));
         }
      }

      public Command CredentialCompleted { get; set; }
      public Command ResetCredential { get; set; }

      public event PropertyChangedEventHandler? PropertyChanged;

      private IDatabase? _database;
      private IUser? _user;

      public MainPageViewModel()
      {
         CredentialCompleted = new Command(_credentialCompleted);
         ResetCredential = new Command(_resetCredential);
      }

      private void _credentialCompleted()
      {
         if (!DatabaseOpened)
         {
            _openDatabase();
            DatabaseOpened = true;
         }
         else
         {
            _login();
         }

         ActualCredential = string.Empty;
      }

      private void _resetCredential()
      {
         _database?.Close();

         _user = null;
         _database = null;

         DatabaseOpened = false;
         ActualCredential = string.Empty;
      }

      private void _openDatabase()
      {
         _database?.Close();
         //_database = Database.Open()
      }

      private void _login()
      {
         _user = _database?.Login(ActualCredential);
      }
   }
}
