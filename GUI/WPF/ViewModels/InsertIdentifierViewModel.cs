using System.Collections.ObjectModel;
using System.ComponentModel;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class InsertIdentifierViewModel(IEnumerable<string> identifiers, string identifier) : INotifyPropertyChanged
   {
      private readonly IEnumerable<string> _identifiers = identifiers;
      private IdentifierType _type = IdentifierTypeDetector.Detect(identifier);
      private string _identifier = identifier;

      public readonly ObservableCollection<string> Identifiers = [.. identifiers.Where(x => x.StartsWith(identifier.Trim(), StringComparison.OrdinalIgnoreCase)),
            .. identifiers.Where(x => x.Contains(identifier.Trim(), StringComparison.OrdinalIgnoreCase)
               && !x.StartsWith(identifier.Trim(), StringComparison.OrdinalIgnoreCase))];

      public IdentifierType Type
      {
         get => _type;
         set
         {
            if (_type == value)
            {
               return;
            }

            _type = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Type)));
         }
      }

      public string Identifier
      {
         get => _identifier;
         set
         {
            value ??= string.Empty;

            if (_identifier == value)
            {
               return;
            }

            _identifier = value;

            if (_type is IdentifierType.Username
               or IdentifierType.Email
               or IdentifierType.PhoneNumber)
            {
               _type = IdentifierTypeDetector.Detect(_identifier);
               PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Type)));
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Identifier)));
            _refreshIdentifiers();
         }
      }

      public IIdentifier ToIdentifier() => new Identifier(Type, Identifier.Trim());

      public event PropertyChangedEventHandler? PropertyChanged;

      private void _refreshIdentifiers()
      {
         Identifiers.Clear();

         string[] matches = [.. _identifiers.Where(x => x.StartsWith(Identifier, StringComparison.OrdinalIgnoreCase)),
            .. _identifiers.Where(x => x.Contains(Identifier, StringComparison.OrdinalIgnoreCase)
               && !x.StartsWith(Identifier, StringComparison.OrdinalIgnoreCase))];

         foreach (string match in matches)
         {
            Identifiers.Add(match);
         }
      }
   }
}
