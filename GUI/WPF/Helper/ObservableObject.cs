using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Base class providing a minimal <see cref="INotifyPropertyChanged"/> implementation
   /// for MVVM, without any external dependency.
   /// </summary>
   internal abstract class ObservableObject : INotifyPropertyChanged
   {
      public event PropertyChangedEventHandler? PropertyChanged;

      /// <summary>
      /// Sets <paramref name="field"/> to <paramref name="newValue"/> and raises
      /// <see cref="PropertyChanged"/> only when the value actually changes.
      /// </summary>
      /// <returns><c>true</c> if the value has been updated; otherwise <c>false</c>.</returns>
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

      /// <summary>
      /// Sets <paramref name="field"/> to <paramref name="newValue"/> and raises
      /// <see cref="PropertyChanged"/> for the property plus every name in
      /// <paramref name="alsoNotify"/>.
      /// </summary>
      protected bool SetProperty<T>(ref T field, T newValue, IEnumerable<string> alsoNotify, [CallerMemberName] string? propertyName = null)
      {
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
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
