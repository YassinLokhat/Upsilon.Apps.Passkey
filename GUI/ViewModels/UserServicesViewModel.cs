using System.ComponentModel;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Helper;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class UserServicesViewModel : INotifyPropertyChanged
   {
      private readonly string _defaultTitle;

      private string _title;
      public string Title
      {
         get => _title;
         set => PropertyHelper.SetProperty(ref _title, value, this, PropertyChanged);
      }

      private bool _isEnabled = true;
      public bool IsEnabled
      {
         get => _isEnabled;
         set => PropertyHelper.SetProperty(ref _isEnabled, value, this, PropertyChanged);
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public UserServicesViewModel(string defaultTitle)
      {
         _title = _defaultTitle = defaultTitle;

         DispatcherTimer timer = new()
         {
            Interval = new TimeSpan(0, 0, 0, 0, 500),
            IsEnabled = true,
         };

         timer.Tick += _timer_Elapsed;
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         string title = _defaultTitle;

         if (MainViewModel.Database is not null)
         {
            if (MainViewModel.Database.HasChanged())
            {
               title += " - *";
            }

            int sessionLeftTime = MainViewModel.Database.SessionLeftTime ?? 0;
            title += $" - Left session time : {sessionLeftTime / 60:D2}:{sessionLeftTime % 60:D2}";
         }

         Title = title;
      }
   }
}
