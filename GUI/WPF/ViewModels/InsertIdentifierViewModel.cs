using System.Collections.ObjectModel;
using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class InsertIdentifierViewModel(IEnumerable<string> identifiers, string identifier) : INotifyPropertyChanged
   {
      private readonly string[] _identifiers = [.. identifiers];

      public readonly ObservableCollection<string> Identifiers = [.. identifiers.Where(x => x.StartsWith(identifier.Trim(), StringComparison.OrdinalIgnoreCase)),
            .. identifiers.Where(x => x.Contains(identifier.Trim(), StringComparison.OrdinalIgnoreCase)
               && !x.StartsWith(identifier.Trim(), StringComparison.OrdinalIgnoreCase))];

      public string Identifier
      {
         get => field.Trim();
         set
         {
            _ = PropertyHelper.SetProperty(ref field, value.Trim(), this, PropertyChanged);
            _refreshFilter();
         }
      } = identifier;

      public event PropertyChangedEventHandler? PropertyChanged;

      private void _onPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      private void _refreshFilter()
      {
         Identifiers.Clear();

         string[] identifiers = [.. _identifiers.Where(x => x.StartsWith(Identifier, StringComparison.OrdinalIgnoreCase)),
            .. _identifiers.Where(x => x.Contains(Identifier, StringComparison.OrdinalIgnoreCase)
               && !x.StartsWith(Identifier, StringComparison.OrdinalIgnoreCase))];

         foreach (string identifier in identifiers)
         {
            Identifiers.Add(identifier);
         }
      }
   }
}
