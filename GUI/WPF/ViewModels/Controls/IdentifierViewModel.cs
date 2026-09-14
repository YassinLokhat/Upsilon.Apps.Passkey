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

   internal sealed class IdentifierViewModel(IAccount account, IIdentifier identifier, bool isNew = false) : ObservableObject, IThemeAware, ILanguageAware
   {
      private readonly IAccount _account = account;
      private readonly bool _isNew = isNew;
      private readonly IdentifierType _baselineType = identifier.Type;
      private readonly string _baselineValue = identifier.Value ?? string.Empty;
      private IdentifierType _type = identifier.Type;

      /// <summary>
      /// Sentinel for "any type" UI filters. Never persist to the vault.
      /// </summary>
      public static IdentifierType AllIdentifierType => (IdentifierType)byte.MaxValue;

      /// <summary>
      /// Glyph for the "All" type filter. Display-only; never persisted.
      /// </summary>
      public const string AllTypeGlyph = "✳️";

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

      public static IReadOnlyList<IdentifierTypeChoice> TypeChoices { get; } = [
         new(IdentifierType.Username, TypeGlyphs[IdentifierType.Username]),
         new(IdentifierType.Email, TypeGlyphs[IdentifierType.Email]),
         new(IdentifierType.PhoneNumber, TypeGlyphs[IdentifierType.PhoneNumber]),
         new(IdentifierType.Passkey, TypeGlyphs[IdentifierType.Passkey]),
         new(IdentifierType.AuthenticatorApp, TypeGlyphs[IdentifierType.AuthenticatorApp]),
      ];

      /// <summary>
      /// Glyph-only choices for the services list identifier-type filter (includes All).
      /// </summary>
      public static IReadOnlyList<IdentifierTypeChoice> FilterTypeChoices { get; } = [
         new(AllIdentifierType, AllTypeGlyph),
         .. TypeChoices,
      ];

      /// <summary>
      /// Localized type label for tooltips; maps <see cref="AllIdentifierType"/> to <see cref="Strings.IdentifierType_All"/>.
      /// </summary>
      public static string GetTypeLabel(IdentifierType type) => type switch
      {
         IdentifierType.Username => Strings.IdentifierType_Username,
         IdentifierType.Email => Strings.IdentifierType_Email,
         IdentifierType.PhoneNumber => Strings.IdentifierType_PhoneNumber,
         IdentifierType.Passkey => Strings.IdentifierType_Passkey,
         IdentifierType.AuthenticatorApp => Strings.IdentifierType_AuthenticatorApp,
         _ when type == AllIdentifierType => Strings.IdentifierType_All,
         _ => string.Empty,
      };

      /// <summary>
      /// Dirty highlight for this row only. Collection-level
      /// <c>HasChanged(Identifiers)</c> is true after any edit, so gray state
      /// compares against the baseline captured at construction.
      /// </summary>
      public Brush IdentifierBackground
      {
         get
         {
            if (!_account.HasChanged(nameof(_account.Identifiers)))
            {
               return FieldStateBrushes.UnchangedBrush2;
            }

            if (_isNew
               || Type != _baselineType
               || !string.Equals(Identifier, _baselineValue, StringComparison.Ordinal))
            {
               return FieldStateBrushes.ChangedBrush;
            }

            return FieldStateBrushes.UnchangedBrush2;
         }
      }

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

      public string TypeLabel => GetTypeLabel(Type);

      /// <summary>
      /// Identifier value only (no type glyph).
      /// </summary>
      public string Identifier
      {
         get; set
         {
            value ??= string.Empty;

            if (field == value)
            {
               return;
            }

            field = value;

            if (_type is IdentifierType.Username
               or IdentifierType.Email
               or IdentifierType.PhoneNumber)
            {
               IdentifierType detected = IdentifierTypeDetector.Detect(field);
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
      } = identifier.Value ?? string.Empty;

      public Identifier ToIdentifier() => new(Type, Identifier);

      public IdentifierViewModel(IAccount account, string value)
         : this(account, new Identifier(IdentifierTypeDetector.Detect(value), value ?? string.Empty), isNew: true)
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
