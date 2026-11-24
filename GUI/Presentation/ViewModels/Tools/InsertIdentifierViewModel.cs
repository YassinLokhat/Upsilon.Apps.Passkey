using System.Collections.ObjectModel;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools
{
   /// <summary>Filters known identifiers (prefix matches first, then substring) for the insert dialog.</summary>
   internal sealed class InsertIdentifierViewModel : ObservableObject
   {
      /// <summary>Sentinel type meaning "any identifier type"; never stored in a vault.</summary>
      public static IdentifierType AllIdentifierType => (IdentifierType)byte.MaxValue;

      private readonly IIdentifier[] _identifiers;

      public ObservableCollection<string> Identifiers { get; } = [];

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
      } = string.Empty;

      public InsertIdentifierViewModel(IEnumerable<IIdentifier> identifiers, IIdentifier? identifier)
      {
         _identifiers = [.. identifiers ?? []];
         Identifier = identifier?.Value ?? string.Empty;
         Type = identifier?.Type ?? IdentifierType.Username;
         _refreshIdentifiers();
      }

      public IIdentifier ToIdentifier() => new Identifier(Type, Identifier.Trim());

      private void _refreshIdentifiers()
      {
         Identifiers.Clear();

         IEnumerable<IIdentifier> typed = _identifiers?.Where(x => _typeMatches(x.Type)) ?? [];
         IEnumerable<string> start = typed
            .Where(x => x.Value.StartsWith(Identifier, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value);
         IEnumerable<string> contains = typed
            .Where(x => x.Value.Contains(Identifier, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value);

         foreach (string match in start.Concat(contains).Distinct())
         {
            Identifiers.Add(match);
         }
      }

      private bool _typeMatches(IdentifierType type)
         => Type == AllIdentifierType || Type == type;
   }
}
