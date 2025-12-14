using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.GUI.Themes;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   public class AccountViewModel(IAccount account) : INotifyPropertyChanged
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

      public string AccountId => $"Account Id : {Account.ItemId.Replace(Account.Service.ItemId, string.Empty)}";

      public Brush LabelBackground => Account.HasChanged(nameof(Label)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Label
      {
         get => Account.Label;
         set
         {
            if (Account.Label != value)
            {
               Account.Label = value;
               OnPropertyChanged(nameof(Label));
            }
         }
      }

      public ObservableCollection<IdentifierViewModel> Identifiers = [];

      public Brush PasswordBackground => Account.HasChanged(nameof(Password)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Password
      {
         get => Account.Password;
         set
         {
            if (Account.Password != value)
            {
               Account.Password = value;
               OnPropertyChanged(nameof(Password));
            }
         }
      }

      public PasswordViewModel[] Passwords => [.. Account.Passwords
            .OrderByDescending(x => x.Key)
            .Select(x => new PasswordViewModel(x.Key.ToShortDateString(), x.Value))];

      public Brush NotesBackground => Account.HasChanged(nameof(Notes)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Notes
      {
         get => Account.Notes;
         set
         {
            if (Account.Notes != value)
            {
               Account.Notes = value;
               OnPropertyChanged(nameof(Notes));
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

               OnPropertyChanged(nameof(RemindPasswordUpdateDelay));
               OnPropertyChanged(nameof(RemindPasswordUpdate));
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
               OnPropertyChanged(nameof(RemindPasswordUpdate));
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

               OnPropertyChanged(nameof(WarnPasswordLeak));
            }
         }
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
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

         OnPropertyChanged(string.Empty);
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
