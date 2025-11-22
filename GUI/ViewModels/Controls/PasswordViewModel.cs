using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

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
