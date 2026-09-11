using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class IdentifierTypeChoice(IdentifierType type, string glyph)
   {
      public IdentifierType Type { get; } = type;

      public string Glyph { get; } = glyph;

      public string Display => Glyph;
   }

   internal sealed class IdentifierViewModel(IAccount account, IIdentifier identifier) : ObservableObject, IThemeAware, ILanguageAware
   {
      private readonly IAccount _account = account;
      private IdentifierType _type = identifier.Type;
      private string _identifier = identifier.Value ?? string.Empty;

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

      private static readonly IReadOnlyList<IdentifierTypeChoice> _typeChoices =
      [
         new(IdentifierType.Username, TypeGlyphs[IdentifierType.Username]),
         new(IdentifierType.Email, TypeGlyphs[IdentifierType.Email]),
         new(IdentifierType.PhoneNumber, TypeGlyphs[IdentifierType.PhoneNumber]),
         new(IdentifierType.Passkey, TypeGlyphs[IdentifierType.Passkey]),
         new(IdentifierType.AuthenticatorApp, TypeGlyphs[IdentifierType.AuthenticatorApp]),
      ];

      public static IReadOnlyList<IdentifierTypeChoice> TypeChoices => _typeChoices;

      public Brush IdentifierBackground => _account.HasChanged("Identifiers") ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;

      public IdentifierType Type
      {
         get => _type;
         set
         {
            if (SetProperty(ref _type, value))
            {
               OnPropertyChanged(nameof(TypeGlyph));
               OnPropertyChanged(nameof(TypeLabel));
               OnPropertyChanged(nameof(IdentifierBackground));
            }
         }
      }

      public string TypeGlyph => TypeGlyphs.TryGetValue(Type, out string? glyph) ? glyph : string.Empty;

      public string TypeLabel => Type switch
      {
         IdentifierType.Username => Strings.IdentifierType_Username,
         IdentifierType.Email => Strings.IdentifierType_Email,
         IdentifierType.PhoneNumber => Strings.IdentifierType_PhoneNumber,
         IdentifierType.Passkey => Strings.IdentifierType_Passkey,
         IdentifierType.AuthenticatorApp => Strings.IdentifierType_AuthenticatorApp,
         _ => string.Empty,
      };

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
                  OnPropertyChanged(nameof(Type));
                  OnPropertyChanged(nameof(TypeGlyph));
                  OnPropertyChanged(nameof(TypeLabel));
               }
            }

            OnPropertyChanged(nameof(Identifier));
            OnPropertyChanged(nameof(IdentifierBackground));
         }
      }

      public Identifier ToIdentifier() => new(Type, Identifier);

      public IdentifierViewModel(IAccount account, string value)
         : this(account, new Identifier(IdentifierTypeDetector.Detect(value), value ?? string.Empty))
      {
      }

      public void Refresh()
      {
         OnPropertyChanged(nameof(IdentifierBackground));
      }

      public void OnThemeChanged() => Refresh();

      public void OnLanguageChanged()
      {
         // TypeChoices is static (glyphs only); refresh the localized tooltip.
         OnPropertyChanged(nameof(TypeLabel));
      }
   }
}
