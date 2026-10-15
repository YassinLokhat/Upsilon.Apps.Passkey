using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Helper
{
   /// <summary>
   /// Minimal <see cref="INotifyPropertyChanged"/> base for MVVM (no third-party packages).
   /// </summary>
   internal abstract class ObservableObject : INotifyPropertyChanged
   {
      public event PropertyChangedEventHandler? PropertyChanged;

      protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
      {
         if (EqualityComparer<T>.Default.Equals(field, newValue))
         {
            return false;
         }

         field = newValue;
         OnPropertyChanged(propertyName);
         return true;
      }

      protected bool SetProperty<T>(ref T field, T newValue, IEnumerable<string> alsoNotify, [CallerMemberName] string? propertyName = null)
      {
         ArgumentNullException.ThrowIfNull(alsoNotify);

         if (!SetProperty(ref field, newValue, propertyName))
         {
            return false;
         }

         foreach (string name in alsoNotify)
         {
            OnPropertyChanged(name);
         }

         return true;
      }

      protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
         => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
   }
}
