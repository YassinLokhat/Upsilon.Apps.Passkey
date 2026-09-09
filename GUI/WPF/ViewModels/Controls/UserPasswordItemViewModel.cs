using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class UserPasswordItemViewModel : INotifyPropertyChanged, IThemeAware, IDisposable
   {
      private int _index;
      private bool _disposed;

      public int Index
      {
         get => _index;
         set
         {
            if (_index == value)
            {
               return;
            }

            _index = value;
            _onPropertyChanged(nameof(Index));
            _onPropertyChanged(nameof(PasskeyLeaked));
            _onPropertyChanged(nameof(PasswordBackground));
         }
      }

      /// <summary>
      /// Seed value used only to initialize the PasswordBox, then cleared.
      /// The live secret lives in the PasswordBox, not in this ViewModel.
      /// </summary>
      public string InitialPassword { get; set; } = string.Empty;

      /// <summary>
      /// True when this onion layer is reported leaked and
      /// <see cref="WarningKinds.PasskeyLeaked"/> is in the user's notify mask.
      /// </summary>
      public bool PasskeyLeaked
         => AppServices.Session.Warnings
            .GetNotifiedWarnings(WarningKinds.PasskeyLeaked)
            .OfType<IPasskeyLeakedWarning>()
            .Any(w => w.PasskeyIndexes.Contains(Index));

      public Brush PasswordBackground
         => SecretFieldBrushes.Background(isDirty: false, isNotifiedLeak: PasskeyLeaked);

      public event PropertyChangedEventHandler? PropertyChanged;

      public UserPasswordItemViewModel()
      {
         AppServices.Session.Warnings.NotifiedWarningsChanged += _onWarningsChanged;
      }

      public void OnThemeChanged()
         => _onPropertyChanged(nameof(PasswordBackground));

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         AppServices.Session.Warnings.NotifiedWarningsChanged -= _onWarningsChanged;
      }

      private void _onWarningsChanged(object? sender, EventArgs e)
         => UiThread.Post(() =>
         {
            _onPropertyChanged(nameof(PasskeyLeaked));
            _onPropertyChanged(nameof(PasswordBackground));
         });

      private void _onPropertyChanged(string propertyName)
         => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
   }
}
