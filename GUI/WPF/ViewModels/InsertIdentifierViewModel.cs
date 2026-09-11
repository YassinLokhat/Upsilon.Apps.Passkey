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

      public IdentifierType Type
      {
         get => field;
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
         get => field;
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
