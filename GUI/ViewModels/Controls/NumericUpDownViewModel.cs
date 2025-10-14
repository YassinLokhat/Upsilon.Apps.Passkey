using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   public class NumericUpDownViewModel : INotifyPropertyChanged
   {
      private uint _value = 0;
      public uint Value
      {
         get => _value;
         set
         {
            if (_value != value)
            {
               _value = value;
               OnPropertyChanged(nameof(Value));
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
