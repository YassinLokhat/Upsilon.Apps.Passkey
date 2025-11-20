using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.Core.Public.Utils;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.PassKey.Core.Public.Enums;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   public class AccountViewModel(IAccount account) : INotifyPropertyChanged
   {
      public readonly IAccount Account = account;

      public string AccountDisplay
      {
         get
         {
            string accountDisplay = $"{Account.Label} {Account.Identifiants.First()}";
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

      public readonly ObservableCollection<IdentifiantViewModel> Identifiants = [];

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

      private void _identifiantViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName != "Identifiant")
         {
            return;
         }

         Account.Identifiants = [.. Identifiants.Select(x => x.Identifiant)];

         foreach (IdentifiantViewModel? identifiant in Identifiants.Except([sender]).Cast<IdentifiantViewModel?>())
         {
            identifiant?.Refresh();
         }

         OnPropertyChanged(string.Empty);
      }

      public void AddIdentifiants()
      {
         Identifiants.Clear();

         if (Account.Identifiants.Length == 0)
         {
            AddIdentifiant(string.Empty);
         }
         else
         {
            foreach (string identifiant in Account.Identifiants)
            {
               AddIdentifiant(identifiant);
            }
         }
      }

      public void AddIdentifiant(string identifiant)
      {
         IdentifiantViewModel identifiantViewModel = new(Account, identifiant);
         identifiantViewModel.PropertyChanged += _identifiantViewModel_PropertyChanged;

         Identifiants.Add(identifiantViewModel);

         _identifiantViewModel_PropertyChanged(null, new("Identifiant"));
      }

      public bool RemoveIdentifiant(IdentifiantViewModel identifiantViewModel)
      {
         if (Identifiants.Count == 1)
         {
            return false;
         }

         _ = Identifiants.Remove(identifiantViewModel);

         _identifiantViewModel_PropertyChanged(null, new("Identifiant"));

         return true;
      }

      public bool MoveIdentifiant(int oldIndex, int newIndex)
      {
         if (oldIndex < 0
            || newIndex < 0
            || newIndex >= Identifiants.Count)
         {
            return false;
         }

         (Identifiants[newIndex], Identifiants[oldIndex]) = (Identifiants[oldIndex], Identifiants[newIndex]);

         _identifiantViewModel_PropertyChanged(null, new("Identifiant"));

         return true;
      }

      public override string ToString() => $"{(Account.HasChanged() ? "* " : string.Empty)}{Account}";
   }
}
