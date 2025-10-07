using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   public class TitleViewModel : INotifyPropertyChanged
   {
      private string _mainWindowTitle = MainWindow.AppTitle;

      public string MainWindowTitle
      {
         get => _mainWindowTitle;
         set
         {
            _mainWindowTitle = value;
            OnPropertyChanged(nameof(MainWindowTitle));
         }
      }

      public string PasswordGeneratorWindowTitle => MainWindowTitle + " - Password Generator";

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
