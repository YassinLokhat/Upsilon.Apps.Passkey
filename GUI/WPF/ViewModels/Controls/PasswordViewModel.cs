using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class PasswordViewModel(string updateDate, string password) : INotifyPropertyChanged
   {
      public string UpdateDate { get; set; } = updateDate;
      public string Password { get; set; } = password;

      public event PropertyChangedEventHandler? PropertyChanged;

      public void Clear()
      {
         Password = string.Empty;
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Password)));
      }
   }
}
