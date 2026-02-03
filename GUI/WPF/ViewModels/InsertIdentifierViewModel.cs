using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Windows.ApplicationModel.VoiceCommands;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class InsertIdentifierViewModel : INotifyPropertyChanged
   {
      private readonly string[] _identifiers;

      public ObservableCollection<string> Identifiers;

      public string Identifier
      {
         get;
         set
         {
            PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
            _refreshFilter();
         }
      } = string.Empty;

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public InsertIdentifierViewModel(IEnumerable<string> identifiers)
      {
         _identifiers = [..identifiers.OrderBy(x => x).Distinct()];
         Identifiers = [.. _identifiers];
      }

      private void _refreshFilter()
      {
         Identifiers.Clear();

         string identifier = Identifier.ToLower().Trim();
         string[] identifiers = [.. _identifiers.Where(x => x.StartsWith(identifier, StringComparison.CurrentCultureIgnoreCase)),
            .. _identifiers.Where(x => x.Contains(identifier, StringComparison.CurrentCultureIgnoreCase)
               && !x.StartsWith(identifier, StringComparison.CurrentCultureIgnoreCase))];

         foreach (string id in identifiers)
         {
            Identifiers.Add(id);
         }
      }
   }
}
