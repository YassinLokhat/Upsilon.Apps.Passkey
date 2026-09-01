using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class QrCodeViewModel : ObservableObject
   {
      private IDispatcherTimer? _timer;

      public QrCodeViewModel()
      {
         BackCommand = new AsyncRelayCommand(() => AppServices.Navigation.GoBackAsync());
      }

      public string Title => Strings.Format(nameof(Strings.Title_QrCode), PasskeyAppInfo.Title);

      public bool[,] Matrix
      {
         get;
         private set
         {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMatrix));
            OnPropertyChanged(nameof(MatrixSize));
         }
      } = new bool[0, 0];

      public bool HasMatrix => Matrix.GetLength(0) > 0;

      public int MatrixSize => Matrix.GetLength(0);

      public ICommand BackCommand { get; }

      public void Load(string content, int autoCloseMs = 0)
      {
         if (string.IsNullOrEmpty(content))
         {
            Matrix = new bool[0, 0];
            return;
         }

         Matrix = QrCode.Generate(content);
         OnPropertyChanged(nameof(Title));

         _timer?.Stop();
         if (autoCloseMs > 0)
         {
            _timer = Application.Current?.Dispatcher.CreateTimer();
            if (_timer is not null)
            {
               _timer.Interval = TimeSpan.FromMilliseconds(autoCloseMs);
               _timer.Tick += async (_, _) =>
               {
                  _timer.Stop();
                  await AppServices.Navigation.GoBackAsync().ConfigureAwait(true);
               };
               _timer.Start();
            }
         }
      }

      public void Unload()
      {
         _timer?.Stop();
         _timer = null;
      }
   }
}
