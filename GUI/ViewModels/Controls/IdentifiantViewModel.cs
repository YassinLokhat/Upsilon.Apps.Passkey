using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.Core.Public.Utils;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   public class IdentifiantViewModel : INotifyPropertyChanged
   {
      private readonly IAccount _account;

      public Brush IdentifiantBackground => _account.HasChanged("Identifiants") ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      private string _identifiant = string.Empty;
      public string Identifiant
      {
         get => _identifiant;
         set
         {
            if (_identifiant != value)
            {
               _identifiant = value;
               OnPropertyChanged(nameof(Identifiant));
            }
         }
      }

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
