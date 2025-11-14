using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.Core.Public.Utils;
using Upsilon.Apps.Passkey.GUI.Helper;
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

      internal bool MeetFilterConditions(string AccountFilter, string identifiantFilter, string textFilter)
         => _matchAccountFilter(AccountFilter.ToLower())
            && _matchIdentifiantFilter(identifiantFilter.ToLower())
            && _matchTextFilter(textFilter.ToLower());

      private bool _matchAccountFilter(string AccountFilter)
      {
         if (string.IsNullOrWhiteSpace(AccountFilter)) return true;

         string AccountId = Account.ItemId.ToLower();
         string AccountName = Account.Label.ToLower();

         if (AccountId.StartsWith(AccountFilter)
            || AccountName.Contains(AccountFilter))
            return true;

         return false;
      }

      private bool _matchIdentifiantFilter(string identifiantFilter)
      {
         if (string.IsNullOrWhiteSpace(identifiantFilter)) return true;

         string AccountId = Account.ItemId.ToLower();
         string AccountName = Account.Label.ToLower();

         if (AccountId.StartsWith(identifiantFilter)
            || AccountName.Contains(identifiantFilter))
            return true;

         return false;
      }

      private bool _matchTextFilter(string textFilter)
      {
         if (string.IsNullOrWhiteSpace(textFilter)) return true;

         string AccountId = Account.ItemId.ToLower();
         string AccountName = Account.Label.ToLower();
         string AccountNote = Account.Notes.ToLower();

         if (AccountId.Contains(textFilter)
            || AccountName.Contains(textFilter)
            || AccountNote.Contains(textFilter))
            return true;

         return false;
      }

      public override string ToString() => $"{(Account.HasChanged() ? "* " : string.Empty)}{Account}";
   }
}
