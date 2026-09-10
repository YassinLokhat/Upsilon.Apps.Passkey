using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class IdentifierViewModel : INotifyPropertyChanged, IThemeAware
   {
      private readonly IAccount _account;
      private IdentifierType _type = IdentifierType.Username;
      private string _identifier = string.Empty;

      /// <summary>
      /// Display-only glyphs for identifier kinds. Never persisted.
      /// </summary>
      public static readonly IReadOnlyDictionary<IdentifierType, string> TypeGlyphs = new Dictionary<IdentifierType, string>
      {
         { IdentifierType.Username, "👤" },
         { IdentifierType.Email, "📧" },
         { IdentifierType.PhoneNumber, "🖁" },
         { IdentifierType.Passkey, "🗝" },
         { IdentifierType.AuthenticatorApp, "📲" },
      };

      public Brush IdentifierBackground => _account.HasChanged("Identifiers") ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;

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
            _onPropertyChanged(nameof(Type));
            _onPropertyChanged(nameof(TypeGlyph));
            _onPropertyChanged(nameof(Identifier));
         }
      }

      public string TypeGlyph => TypeGlyphs.TryGetValue(Type, out string? glyph) ? glyph : string.Empty;

      /// <summary>
      /// Identifier value only (no type glyph).
      /// </summary>
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
               _onPropertyChanged(nameof(Type));
               _onPropertyChanged(nameof(TypeGlyph));
            }

            _onPropertyChanged(nameof(Identifier));
         }
      }

      public Identifier ToIdentifier() => new(Type, Identifier);

      public event PropertyChangedEventHandler? PropertyChanged;

      private void _onPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IdentifierBackground)));
      }

      public IdentifierViewModel(IAccount account, IIdentifier identifier)
      {
         _account = account;
         _type = identifier.Type;
         _identifier = identifier.Value ?? string.Empty;
      }

      public IdentifierViewModel(IAccount account, string value)
         : this(account, new Identifier(IdentifierTypeDetector.Detect(value), value ?? string.Empty))
      {
      }

      public void Refresh()
      {
         _onPropertyChanged(nameof(IdentifierBackground));
      }

      public void OnThemeChanged() => Refresh();
   }
}
