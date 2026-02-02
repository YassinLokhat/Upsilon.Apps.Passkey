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
