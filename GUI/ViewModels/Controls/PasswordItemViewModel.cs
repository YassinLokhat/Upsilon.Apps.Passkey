using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   internal class PasswordItemViewModel : INotifyPropertyChanged
   {
      private int _index = 0;
      public int Index
      {
         get => _index;
         set
         {
            if (_index != value)
            {
               _index = value;
               OnPropertyChanged(nameof(Index));
            }
         }
      }

      private string _password = string.Empty;
      public string Password
      {
         get => _password;
         set
         {
            if (_password != value)
            {
               _password = value;
               OnPropertyChanged(nameof(Password));
            }
         }
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
