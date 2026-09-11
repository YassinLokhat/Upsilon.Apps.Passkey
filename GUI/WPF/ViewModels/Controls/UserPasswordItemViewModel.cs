using System.Windows.Input;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class UserPasswordItemViewModel : ObservableObject, IThemeAware, IDisposable
   {
      private bool _disposed;

      public int Index
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(PasskeyLeaked));
               OnPropertyChanged(nameof(PasswordBackground));
            }
         }
      }

      /// <summary>
      /// Seed value used only to initialize the PasswordBox, then cleared.
      /// The live secret lives in the PasswordBox, not in this ViewModel.
      /// </summary>
      public string InitialPassword { get; set; } = string.Empty;

      /// <summary>
      /// True when this onion layer is reported leaked and
      /// <see cref="AlertKinds.PasskeyLeaked"/> is in the user's notify mask.
      /// </summary>
      public bool PasskeyLeaked
         => AppServices.Session.Alerts
            .GetNotifiedAlerts(AlertKinds.PasskeyLeaked)
            .OfType<IPasskeyLeakedAlert>()
            .Any(w => w.PasskeyIndexes.Contains(Index));

      public Brush PasswordBackground
         => SecretFieldBrushes.Background(isDirty: false, isNotifiedLeak: PasskeyLeaked);

      public ICommand UpCommand { get; }
      public ICommand DownCommand { get; }
      public ICommand DeleteCommand { get; }

      public event EventHandler? UpRequested;
      public event EventHandler? DownRequested;
      public event EventHandler? DeleteRequested;

      public UserPasswordItemViewModel()
      {
         UpCommand = new RelayCommand(() => UpRequested?.Invoke(this, EventArgs.Empty));
         DownCommand = new RelayCommand(() => DownRequested?.Invoke(this, EventArgs.Empty));
         DeleteCommand = new RelayCommand(() => DeleteRequested?.Invoke(this, EventArgs.Empty));

         AppServices.Session.Alerts.NotifiedAlertsChanged += _onAlertsChanged;
      }

      public void OnThemeChanged()
         => OnPropertyChanged(nameof(PasswordBackground));

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         AppServices.Session.Alerts.NotifiedAlertsChanged -= _onAlertsChanged;
         UpRequested = null;
         DownRequested = null;
         DeleteRequested = null;
      }

      private void _onAlertsChanged(object? sender, EventArgs e)
         => UiThread.Post(() =>
         {
            OnPropertyChanged(nameof(PasskeyLeaked));
            OnPropertyChanged(nameof(PasswordBackground));
         });
   }
}
