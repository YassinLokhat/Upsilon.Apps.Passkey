using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class UserPasswordItemViewModel : INotifyPropertyChanged
   {
      public int Index
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      }

      /// <summary>
      /// Seed value used only to initialize the PasswordBox, then cleared.
      /// The live secret lives in the PasswordBox, not in this ViewModel.
      /// </summary>
      public string InitialPassword { get; set; } = string.Empty;

      public event PropertyChangedEventHandler? PropertyChanged;
   }
}
