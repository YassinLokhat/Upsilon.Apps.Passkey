using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class AccountViewModel(IAccount account) : INotifyPropertyChanged, IThemeAware
   {
      public readonly IAccount Account = account;

      public string AccountDisplay
      {
         get
         {
            string accountDisplay = $"{Account.Label} {Account.Identifiers.First()}";
            return $"{(Account.HasChanged() ? "* " : string.Empty)}{accountDisplay.Trim()}";
         }
      }

      public string AccountId => Strings.Format(nameof(Strings.Msg_AccountId), Account.ItemId);

      public Brush LabelBackground => Account.HasChanged(nameof(Label)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Label
      {
         get => Account.Label;
         set
         {
            if (Account.Label != value)
            {
               Account.Label = value;
               _onPropertyChanged(nameof(Label));
            }
         }
      }

      public ObservableCollection<IdentifierViewModel> Identifiers = [];

      public Brush PasswordBackground => Account.HasChanged(nameof(Password)) ? DarkMode.ChangedBrush : !PasswordLeaked ? DarkMode.UnchangedBrush2 : SemanticBrushes.Danger;
      public string Password
      {
         get => Account.Password;
         set
         {
            if (Account.Password != value)
            {
               Account.Password = value;
               _onPropertyChanged(nameof(Password));
            }
         }
      }

      public PasswordViewModel[] Passwords
      {
         get
         {
            PasswordViewModel[] passwords = [.. Account.Passwords
               .OrderByDescending(x => x.Key)
               .Select(x => new PasswordViewModel(x.Key.ToString(Strings.Activity_DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture), x.Value))];

            if (passwords.Length != 0
               && string.IsNullOrEmpty(passwords.Last().Password))
            {
               passwords = passwords[..(passwords.Length - 1)];
            }

            return passwords;
         }
      }

      public Brush NotesBackground => Account.HasChanged(nameof(Notes)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Notes
      {
         get => Account.Notes;
         set
         {
            if (Account.Notes != value)
            {
               Account.Notes = value;
               _onPropertyChanged(nameof(Notes));
            }
         }
      }

      public int RemindPasswordUpdateDelay
      {
         get => Account.PasswordUpdateReminderDelay;
         set
         {
            if (Account.PasswordUpdateReminderDelay != value)
            {
               Account.PasswordUpdateReminderDelay = value;

               _onPropertyChanged(nameof(RemindPasswordUpdateDelay));
               _onPropertyChanged(nameof(RemindPasswordUpdate));
            }
         }
      }

      public bool RemindPasswordUpdate
      {
         get => RemindPasswordUpdateDelay != 0;
         set
         {
            if (RemindPasswordUpdate != value)
            {
               RemindPasswordUpdateDelay = value ? 2 : 0;
               _onPropertyChanged(nameof(RemindPasswordUpdate));
            }
         }
      }

      public bool WarnPasswordLeak
      {
         get => Account.Options.HasFlag(AccountOption.WarnIfPasswordLeaked);
         set
         {
            if (WarnPasswordLeak != value)
            {
               if (value)
               {
                  Account.Options |= AccountOption.WarnIfPasswordLeaked;
               }
               else
               {
                  Account.Options &= ~AccountOption.WarnIfPasswordLeaked;
               }

               _onPropertyChanged(nameof(WarnPasswordLeak));
            }
         }
      }

      public bool WarnIfDuplicatedPassword
      {
         get => Account.Options.HasFlag(AccountOption.WarnIfDuplicatedPassword);
         set
         {
            if (WarnIfDuplicatedPassword != value)
            {
               if (value)
               {
                  Account.Options |= AccountOption.WarnIfDuplicatedPassword;
               }
               else
               {
                  Account.Options &= ~AccountOption.WarnIfDuplicatedPassword;
               }

               _onPropertyChanged(nameof(WarnIfDuplicatedPassword));
            }
         }
      }

      public bool PasswordLeaked
         => Account.Options.HasFlag(AccountOption.WarnIfPasswordLeaked)
               && AppServices.Session.Database?.Warnings is not null
               && AppServices.Session.Database.Warnings.Any(x => x.WarningType == WarningType.PasswordLeakedWarning
                  && (x.Accounts?.Contains(Account) ?? false));

      public static string[] IdentifierAutoCompleteList => AppServices.Session.User?.Services
         .SelectMany(x => x.Accounts)
         .SelectMany(x => x.Identifiers)
         .Distinct()
         .Where(x => !string.IsNullOrEmpty(x))
         .OrderBy(x => x)
         .ToArray() ?? [];

      public event PropertyChangedEventHandler? PropertyChanged;

      public void OnLanguageChanged()
         => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountId)));

      public void OnThemeChanged()
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LabelBackground)));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PasswordBackground)));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotesBackground)));

         foreach (IdentifierViewModel identifier in Identifiers)
         {
            identifier.OnThemeChanged();
         }
      }

      private void _onPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{propertyName}Background"));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountDisplay)));
      }

      private void _identifierViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName != "Identifier")
         {
            return;
         }

         Account.Identifiers = [.. Identifiers.Select(x => x.Identifier)];

         foreach (IdentifierViewModel? identifier in Identifiers.Except([sender]).Cast<IdentifierViewModel?>())
         {
            identifier?.Refresh();
         }

         _onPropertyChanged(string.Empty);
      }

      public void AddIdentifier(IdentifierViewModel identifierViewModel)
      {
         identifierViewModel.PropertyChanged += _identifierViewModel_PropertyChanged;

         _identifierViewModel_PropertyChanged(null, new("Identifier"));
      }

      public void AddIdentifier(string identifier)
      {
         IdentifierViewModel identifierViewModel = new(Account, identifier);
         identifierViewModel.PropertyChanged += _identifierViewModel_PropertyChanged;

         Identifiers.Add(identifierViewModel);

         _identifierViewModel_PropertyChanged(null, new("Identifier"));
      }

      public bool RemoveIdentifier(IdentifierViewModel identifierViewModel)
      {
         if (Identifiers.Count == 1)
         {
            return false;
         }

         _ = Identifiers.Remove(identifierViewModel);

         _identifierViewModel_PropertyChanged(null, new("Identifier"));

         return true;
      }

      public bool MoveIdentifier(int oldIndex, int newIndex)
      {
         if (oldIndex < 0
            || newIndex < 0
            || newIndex >= Identifiers.Count)
         {
            return false;
         }

         (Identifiers[newIndex], Identifiers[oldIndex]) = (Identifiers[oldIndex], Identifiers[newIndex]);

         _identifierViewModel_PropertyChanged(null, new("Identifier"));

         return true;
      }

      public override string ToString() => $"{(Account.HasChanged() ? "* " : string.Empty)}{Account}";
   }
}
