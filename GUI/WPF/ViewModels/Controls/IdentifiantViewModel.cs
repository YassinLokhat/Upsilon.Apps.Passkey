using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed partial class IdentifierViewModel : INotifyPropertyChanged, IThemeAware
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
               if (IdentifiersTypes.Keys.Union(IdentifiersTypes.Values).All(x => !value.StartsWith(x, StringComparison.Ordinal)))
               {
                  value = _getIdentifierType(value);
               }

               foreach (KeyValuePair<string, string> idType in IdentifiersTypes)
               {
                  field = value.Replace(idType.Key, idType.Value, StringComparison.Ordinal);
               }

               _onPropertyChanged(nameof(Identifier));
            }
         }
      } = string.Empty;

      public event PropertyChangedEventHandler? PropertyChanged;

      private void _onPropertyChanged(string propertyName)
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
         _onPropertyChanged(nameof(IdentifierBackground));
      }

      public void OnThemeChanged() => Refresh();

      [GeneratedRegex(@"^\+\d{1,3}[\d\s\-\.]{6,20}$")]
      private static partial Regex _phoneRegex();
      [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
      private static partial Regex _mailRegex();
      private static string _getIdentifierType(string identifier)
      {
         return _phoneRegex().IsMatch(identifier) ? "🖁" + identifier
            : _mailRegex().IsMatch(identifier) ? "📧" + identifier
            : "👤" + identifier;
      }
   }
}
