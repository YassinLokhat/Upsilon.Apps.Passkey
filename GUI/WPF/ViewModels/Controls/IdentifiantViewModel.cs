using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   public class IdentifierViewModel : INotifyPropertyChanged
   {
      private readonly IAccount _account;

      public static readonly Dictionary<string, string> IdentifiersTypes = new()
      {
         { "[Username]", "👤" },
         { "[Email]", "📧" },
         { "[Phone Number]", "🖁" },
         { "[Passkey]", "🗝" },
         { "[Authentificator App]", "📲" },
      };

      public Brush IdentifierBackground => _account.HasChanged("Identifiers") ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;

      public string Identifier
      {
         get;
         set
         {
            if (field != value)
            {
               if (IdentifiersTypes.Keys.Union(IdentifiersTypes.Values).All(x => !value.StartsWith(x, StringComparison.CurrentCultureIgnoreCase)))
               {
                  value = _getIdentifierType(value);
               }

               foreach (KeyValuePair<string, string> idType in IdentifiersTypes)
               {
                  field = value.Replace(idType.Key, idType.Value);
               }

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

      private static string _getIdentifierType(string identifier)
      {
         return Regex.IsMatch(identifier, @"^\+\d{1,3}[\d\s\-\.]{6,20}$") ? "🖁" + identifier
            : Regex.IsMatch(identifier, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$") ? "📧" + identifier
            : "👤" + identifier;
      }
   }
}
