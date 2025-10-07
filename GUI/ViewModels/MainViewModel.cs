using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   public class MainViewModel : INotifyPropertyChanged
   {
      private string _windowTitle = MainWindow.AppTitle;

      public string WindowTitle
      {
         get => _windowTitle;
         set
         {
            _windowTitle = value;
            OnPropertyChanged(nameof(WindowTitle));
         }
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
