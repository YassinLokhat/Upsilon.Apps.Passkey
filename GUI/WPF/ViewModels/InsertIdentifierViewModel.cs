using System.Collections.ObjectModel;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class InsertIdentifierViewModel : ObservableObject
   {
      private readonly IEnumerable<IIdentifier> _identifiers;

      public readonly ObservableCollection<string> Identifiers = [];

      public static IdentifierType AllIdentifierType => (IdentifierType)byte.MaxValue;

      public IdentifierType Type
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _refreshIdentifiers();
            }
         }
      }

      public string Identifier
      {
         get;
         set
         {
            value ??= string.Empty;

            if (SetProperty(ref field, value))
            {
               _refreshIdentifiers();
            }
         }
      }

      public InsertIdentifierViewModel(IEnumerable<IIdentifier> identifiers, IIdentifier identifier)
      {
         _identifiers = identifiers;
         Identifier = identifier.Value;
         Type = identifier.Type;
      }

      public IIdentifier ToIdentifier() => new Identifier(Type, Identifier.Trim());

      private void _refreshIdentifiers()
      {
         Identifiers.Clear();

         IIdentifier[] start = [.. _identifiers.Where(x => _typeMatches(x.Type) && x.Value.StartsWith(Identifier, StringComparison.OrdinalIgnoreCase))];

         IIdentifier[] contains = [.. _identifiers.Where(x => _typeMatches(x.Type)
            && x.Value.Contains(Identifier, StringComparison.OrdinalIgnoreCase)
            && !x.Value.StartsWith(Identifier, StringComparison.OrdinalIgnoreCase))];

         string[] matches = [.. start.Union(contains).Select(x => x.Value).Distinct()];

         foreach (string match in matches)
         {
            Identifiers.Add(match);
         }
      }

      private bool _typeMatches(IdentifierType type)
         => Type == AllIdentifierType || Type == type;
   }
}
