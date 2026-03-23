using System.Collections.ObjectModel;
using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class InsertIdentifierViewModel(IEnumerable<string> identifiers, string identifier) : INotifyPropertyChanged
   {
      private readonly string[] _identifiers = [.. identifiers];

      public ObservableCollection<string> Identifiers = [.. identifiers.Where(x => x.StartsWith(identifier.Trim(), StringComparison.CurrentCultureIgnoreCase)),
            .. identifiers.Where(x => x.Contains(identifier.Trim(), StringComparison.CurrentCultureIgnoreCase)
               && !x.StartsWith(identifier.Trim(), StringComparison.CurrentCultureIgnoreCase))];

      public string Identifier
      {
         get => field.Trim();
         set
         {
            PropertyHelper.SetProperty(ref field, value.Trim(), this, PropertyChanged);
            _refreshFilter();
         }
      } = identifier;

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      private void _refreshFilter()
      {
         Identifiers.Clear();

         string[] identifiers = [.. _identifiers.Where(x => x.StartsWith(Identifier, StringComparison.CurrentCultureIgnoreCase)),
            .. _identifiers.Where(x => x.Contains(Identifier, StringComparison.CurrentCultureIgnoreCase)
               && !x.StartsWith(Identifier, StringComparison.CurrentCultureIgnoreCase))];

         foreach (string identifier in identifiers)
         {
            Identifiers.Add(identifier);
         }
      }
   }
}
