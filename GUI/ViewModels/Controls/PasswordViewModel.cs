using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   public class PasswordViewModel(string updateDate, string password) : INotifyPropertyChanged
   {
      public string UpdateDate { get; set; } = updateDate;
      public string Password { get; set; } = password;

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
