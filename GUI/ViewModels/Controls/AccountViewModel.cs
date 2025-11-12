using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.Core.Public.Utils;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   internal class AccountViewModel(IAccount Account) : INotifyPropertyChanged
   {
      private readonly IAccount _account = Account;

      public string AccountDisplay => $"{(_account.HasChanged() ? "* " : string.Empty)}{_account}";

      public string AccountId => $"Account Id : {_account.ItemId.Replace(_account.Service.ItemId, string.Empty)}";

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
