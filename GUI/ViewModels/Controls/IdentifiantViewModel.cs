using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.Core.Public.Interfaces;
using Upsilon.Apps.Passkey.Core.Public.Utils;
using Upsilon.Apps.Passkey.GUI.Themes;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   public class IdentifiantViewModel : INotifyPropertyChanged
   {
      private readonly IAccount _account;

      public Brush IdentifiantBackground => _account.HasChanged("Identifiants") ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;

      public string Identifiant
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               OnPropertyChanged(nameof(Identifiant));
            }
         }
      } = string.Empty;

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{propertyName}Background"));
      }

      public IdentifiantViewModel(IAccount account, string identifiant)
      {
         _account = account;
         Identifiant = identifiant;
      }

      public void Refresh()
      {
         OnPropertyChanged(nameof(IdentifiantBackground));
      }
   }
}
