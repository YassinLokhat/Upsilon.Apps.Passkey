using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class MainPageViewModel : INotifyPropertyChanged
   {
      public static string AppTitle => Helper.AppInfo.Title;

      /// <summary>
      /// Window title, optionally including the login idle countdown suffix.
      /// </summary>
      public string WindowTitle
      {
         get;
         set
         {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowTitle)));
         }
      } = AppTitle;

      public string CredentialsLabel
      {
         get;
         set
         {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CredentialsLabel)));
         }
      } = "Username : ";

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
         }
      }

      public Command CredentialCompleted { get; set; }
      public Command ResetCredential { get; set; }

      public event PropertyChangedEventHandler? PropertyChanged;

      public MainPageViewModel()
      {
         CredentialCompleted = new Command(_credentialCompleted);
         ResetCredential = new Command(_resetCredential);
      }

      private void _credentialCompleted()
      {
         ActualCredential = string.Empty;
         DatabaseOpened = true;
      }

      private void _resetCredential()
      {
         ActualCredential = string.Empty;
         DatabaseOpened = false;
      }
   }
}
