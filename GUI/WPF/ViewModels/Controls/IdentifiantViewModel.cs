using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class IdentifierTypeChoice(IdentifierType type, string glyph, string label)
   {
      public IdentifierType Type { get; } = type;

      public string Glyph { get; } = glyph;

      public string Label { get; } = label;

      public string Display => Glyph;
   }

   internal sealed class IdentifierViewModel : INotifyPropertyChanged, IThemeAware, ILanguageAware
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

      public static IReadOnlyList<IdentifierTypeChoice> CreateTypeChoices()
         =>
         [
            new(IdentifierType.Username, TypeGlyphs[IdentifierType.Username], Strings.IdentifierType_Username),
            new(IdentifierType.Email, TypeGlyphs[IdentifierType.Email], Strings.IdentifierType_Email),
            new(IdentifierType.PhoneNumber, TypeGlyphs[IdentifierType.PhoneNumber], Strings.IdentifierType_PhoneNumber),
            new(IdentifierType.Passkey, TypeGlyphs[IdentifierType.Passkey], Strings.IdentifierType_Passkey),
            new(IdentifierType.AuthenticatorApp, TypeGlyphs[IdentifierType.AuthenticatorApp], Strings.IdentifierType_AuthenticatorApp),
         ];

      private IReadOnlyList<IdentifierTypeChoice> _typeChoices = CreateTypeChoices();

      public IReadOnlyList<IdentifierTypeChoice> TypeChoices => _typeChoices;

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
            _onPropertyChanged(nameof(TypeLabel));
         }
      }

      public string TypeGlyph => TypeGlyphs.TryGetValue(Type, out string? glyph) ? glyph : string.Empty;

      public string TypeLabel
         => _typeChoices.FirstOrDefault(x => x.Type == Type)?.Label ?? string.Empty;

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
               IdentifierType detected = IdentifierTypeDetector.Detect(_identifier);
               if (_type != detected)
               {
                  _type = detected;
                  _onPropertyChanged(nameof(Type));
                  _onPropertyChanged(nameof(TypeGlyph));
                  _onPropertyChanged(nameof(TypeLabel));
               }
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

      public void OnLanguageChanged()
      {
         _typeChoices = CreateTypeChoices();
         _onPropertyChanged(nameof(TypeChoices));
         _onPropertyChanged(nameof(TypeLabel));
      }
   }
}
