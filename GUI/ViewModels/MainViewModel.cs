using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class MainViewModel : INotifyPropertyChanged
   {
      public static string AppTitle
      {
         get
         {
            var package = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            var packageVersion = package.Version?.ToString(2);

            return $"{package.Name} v{packageVersion}";
         }
      }

      private string _mainWindowTitle = AppTitle;

      public string MainWindowTitle
      {
         get => _mainWindowTitle;
         set
         {
            if (_mainWindowTitle != value)
            {
               _mainWindowTitle = value;
               OnPropertyChanged(nameof(MainWindowTitle));
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
