using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   public class IdentifierViewModel : INotifyPropertyChanged
   {
      private readonly IAccount _account;

      public Brush IdentifierBackground => _account.HasChanged("Identifiers") ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string[] IdentifierAutoCompleteList => _account.Database.User?.Services
         .SelectMany(x => x.Accounts)
         .SelectMany(x => x.Identifiers)
         .Distinct()
         .OrderBy(x => x)
         .ToArray() ?? [];

      public string Identifier
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               OnPropertyChanged(nameof(Identifier));
            }
         }
      } = string.Empty;

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{propertyName}Background"));
      }

      public IdentifierViewModel(IAccount account, string identifier)
      {
         _account = account;
         Identifier = identifier;
      }

      public void Refresh()
      {
         OnPropertyChanged(nameof(IdentifierBackground));
      }
   }
}
