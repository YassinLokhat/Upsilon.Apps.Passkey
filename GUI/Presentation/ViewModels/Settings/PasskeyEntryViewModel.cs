using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings
{
   /// <summary>One passkey row of <see cref="PasskeyListViewModel"/>.</summary>
   internal sealed class PasskeyEntryViewModel : ObservableObject, IDisposable
   {
      private IProtectedSecret? _seed;
      private ISecretInput? _input;
      private bool _disposed;

      public PasskeyEntryViewModel(IProtectedSecret? seed, int? storedIndex)
      {
         _seed = seed;
         StoredIndex = storedIndex;

         UpCommand = new RelayCommand(() => UpRequested?.Invoke(this, EventArgs.Empty));
         DownCommand = new RelayCommand(() => DownRequested?.Invoke(this, EventArgs.Empty));
         DeleteCommand = new RelayCommand(() => DeleteRequested?.Invoke(this, EventArgs.Empty));

         AppServices.Session.Alerts.NotifiedAlertsChanged += _onAlertsChanged;
      }

      public int Index
      {
         get;
         set => SetProperty(ref field, value);
      }

      /// <summary>
      /// Position of this passkey in the saved vault; <c>null</c> for a new row.
      /// Alerts index the saved layers, so badges follow this, not <see cref="Index"/>.
      /// </summary>
      public int? StoredIndex { get; }

      public bool PasskeyLeaked
         => StoredIndex is int stored
            && AppServices.Session.Alerts
               .GetNotifiedAlerts(AlertKinds.PasskeyLeaked)
               .OfType<IPasskeyLeakedAlert>()
               .Any(w => w.PasskeyIndexes.Contains(stored));

      public bool PasskeyWeak
         => StoredIndex is int stored
            && AppServices.Session.Alerts
               .GetNotifiedAlerts(AlertKinds.WeakPasskey)
               .OfType<IWeakPasskeyAlert>()
               .Any(w => w.PasskeyIndexes.Contains(stored));

      public FieldVisualState PasswordState
         => FieldVisualStateHelper.ForEditedSecret(isDirty: false, isNotifiedWeak: PasskeyWeak, isNotifiedLeak: PasskeyLeaked);

      public ICommand UpCommand { get; }
      public ICommand DownCommand { get; }
      public ICommand DeleteCommand { get; }

      public event EventHandler? UpRequested;
      public event EventHandler? DownRequested;
      public event EventHandler? DeleteRequested;

      /// <summary>Binds the host control and moves the stored secret into it.</summary>
      public void Attach(ISecretInput input)
      {
         ArgumentNullException.ThrowIfNull(input);
         if (ReferenceEquals(_input, input))
         {
            return;
         }

         // Hosts may recreate the row control (reorder, re-template): carry the typed secret over.
         if (_input is { } previous)
         {
            input.Write(previous.Read());
            previous.Clear();
         }

         _input = input;

         if (_seed is not null)
         {
            input.Write(_seed.Reveal());
            _seed = null;
         }
      }

      /// <summary>Plaintext for one immediate use; never store the result.</summary>
      public string Reveal() => _input?.Read() ?? _seed?.Reveal() ?? string.Empty;

      public void Clear()
      {
         _input?.Clear();
         _seed = null;
      }

      public void OnThemeChanged()
         => OnPropertyChanged(nameof(PasswordState));

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         Clear();
         _input = null;
         AppServices.Session.Alerts.NotifiedAlertsChanged -= _onAlertsChanged;
         UpRequested = null;
         DownRequested = null;
         DeleteRequested = null;
      }

      private void _onAlertsChanged(object? sender, EventArgs e)
         => UiThread.Post(() =>
         {
            OnPropertyChanged(nameof(PasskeyLeaked));
            OnPropertyChanged(nameof(PasskeyWeak));
            OnPropertyChanged(nameof(PasswordState));
         });
   }
}
