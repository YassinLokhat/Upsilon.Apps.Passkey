using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.Helper;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   public class PasswordItemViewModel : INotifyPropertyChanged
   {
      private int _index = 0;
      public int Index
      {
         get => _index;
         set => PropertyHelper.SetProperty(ref _index, value, this, PropertyChanged);
      }

      private string _password = string.Empty;
      public string Password
      {
         get => _password;
         set => PropertyHelper.SetProperty(ref _password, value, this, PropertyChanged);
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
