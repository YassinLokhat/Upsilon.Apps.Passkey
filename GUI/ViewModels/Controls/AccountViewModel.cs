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
      private readonly IAccount _account = account;

      public string AccountDisplay
      {
         get
         {
            string accountDisplay = $"{_account.Label} {_account.Identifiants.First()}";
            return $"{(_account.HasChanged() ? "* " : string.Empty)}{accountDisplay.Trim()}";
         }
      }

      public string AccountId => $"_account Id : {_account.ItemId.Replace(_account.Service.ItemId, string.Empty)}";

      public Brush LabelBackground => _account.HasChanged(nameof(Label)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Label
      {
         get => _account.Label;
         set
         {
            if (_account.Label != value)
            {
               _account.Label = value;
               OnPropertyChanged(nameof(Label));
            }
         }
      }

      public readonly ObservableCollection<IdentifiantViewModel> Identifiants = [];

      public Brush NotesBackground => _account.HasChanged(nameof(Notes)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Notes
      {
         get => _account.Notes;
         set
         {
            if (_account.Notes != value)
            {
               _account.Notes = value;
               OnPropertyChanged(nameof(Notes));
            }
         }
      }

      public int RemindPasswordUpdateDelay
      {
         get => _account.PasswordUpdateReminderDelay;
         set
         {
            if (_account.PasswordUpdateReminderDelay != value)
            {
               _account.PasswordUpdateReminderDelay = value;

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
         get => _account.Options.HasFlag(AccountOption.WarnIfPasswordLeaked);
         set
         {
            if (WarnPasswordLeak != value)
            {
               if (value)
               {
                  _account.Options |= AccountOption.WarnIfPasswordLeaked;
               }
               else
               {
                  _account.Options &= ~AccountOption.WarnIfPasswordLeaked;
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

         _account.Identifiants = [.. Identifiants.Select(x => x.Identifiant)];

         foreach (IdentifiantViewModel? identifiant in Identifiants.Except([sender]))
         {
            identifiant?.Refresh();
         }

         OnPropertyChanged(string.Empty);
      }

      public void AddIdentifiants()
      {
         Identifiants.Clear();

         if (_account.Identifiants.Length == 0)
         {
            AddIdentifiant(string.Empty);
         }
         else
         {
            foreach (string identifiant in _account.Identifiants)
            {
               AddIdentifiant(identifiant);
            }
         }
      }

      public void AddIdentifiant(string identifiant)
      {
         IdentifiantViewModel identifiantViewModel = new(_account, identifiant);
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

      internal bool MeetFilterConditions(string AccountFilter, string identifiantFilter, string textFilter)
         => _matchAccountFilter(AccountFilter.ToLower())
            && _matchIdentifiantFilter(identifiantFilter.ToLower())
            && _matchTextFilter(textFilter.ToLower());

      private bool _matchAccountFilter(string AccountFilter)
      {
         if (string.IsNullOrWhiteSpace(AccountFilter)) return true;

         string AccountId = _account.ItemId.ToLower();
         string AccountName = _account.Label.ToLower();

         if (AccountId.StartsWith(AccountFilter)
            || AccountName.Contains(AccountFilter))
            return true;

         return false;
      }

      private bool _matchIdentifiantFilter(string identifiantFilter)
      {
         if (string.IsNullOrWhiteSpace(identifiantFilter)) return true;

         string AccountId = _account.ItemId.ToLower();
         string AccountName = _account.Label.ToLower();

         if (AccountId.StartsWith(identifiantFilter)
            || AccountName.Contains(identifiantFilter))
            return true;

         return false;
      }

      private bool _matchTextFilter(string textFilter)
      {
         if (string.IsNullOrWhiteSpace(textFilter)) return true;

         string AccountId = _account.ItemId.ToLower();
         string AccountName = _account.Label.ToLower();
         string AccountNote = _account.Notes.ToLower();

         if (AccountId.Contains(textFilter)
            || AccountName.Contains(textFilter)
            || AccountNote.Contains(textFilter))
            return true;

         return false;
      }

      public override string ToString() => $"{(_account.HasChanged() ? "* " : string.Empty)}{_account}";
   }
}
