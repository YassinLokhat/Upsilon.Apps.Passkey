using System.Collections.ObjectModel;
using System.ComponentModel;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class InsertIdentifierViewModel : INotifyPropertyChanged
   {
      private readonly IEnumerable<IIdentifier> _identifiers;

      public readonly ObservableCollection<string> Identifiers = [];

      public IdentifierType Type
      {
         get => field;
         set
         {
            if (field == value)
            {
               return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Type)));
            _refreshIdentifiers();
         }
      }

      public string Identifier
      {
         get => field;
         set
         {
            value ??= string.Empty;

            if (field == value)
            {
               return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Identifier)));
            _refreshIdentifiers();
         }
      }

      public InsertIdentifierViewModel(IEnumerable<IIdentifier> identifiers, IIdentifier identifier)
      {
         _identifiers = identifiers;
         Identifier = identifier.Value;
         Type = identifier.Type;
      }

      public IIdentifier ToIdentifier() => new Identifier(Type, Identifier.Trim());

      public event PropertyChangedEventHandler? PropertyChanged;

      private void _refreshIdentifiers()
      {
         Identifiers.Clear();

         IIdentifier[] start = [.. _identifiers.Where(x => x.Type == Type && x.Value.StartsWith(Identifier, StringComparison.OrdinalIgnoreCase))];

         IIdentifier[] contains = [.. _identifiers.Where(x => x.Type == Type
            && x.Value.Contains(Identifier, StringComparison.OrdinalIgnoreCase)
            && !x.Value.StartsWith(Identifier, StringComparison.OrdinalIgnoreCase))];

         IIdentifier[] matches = [.. start, .. contains];

         foreach (IIdentifier match in matches)
         {
            Identifiers.Add(match.Value);
         }
      }
   }
}
