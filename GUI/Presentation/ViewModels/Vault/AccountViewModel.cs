using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;


namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Vault
{
   internal sealed class AccountViewModel : ObservableObject, IDisposable
   {
      public readonly IAccount Account;
      private bool _disposed;

      public AccountViewModel(IAccount account)
      {
         Account = account;
         AppServices.Session.Alerts.NotifiedAlertsChanged += _onAlertsChanged;

         AddIdentifierCommand = new AsyncRelayCommand(_addIdentifierAsync);
         MoveIdentifierUpCommand = new RelayCommand(_moveIdentifierUp);
         MoveIdentifierDownCommand = new RelayCommand(_moveIdentifierDown);
         DeleteIdentifierCommand = new RelayCommand(_deleteIdentifier);
         CopyIdentifierCommand = new RelayCommand(_copyIdentifier);
         ShowQrCodeIdentifierCommand = new AsyncRelayCommand(() => ClipboardPolicy.ShowQrCodeAsync(SelectedIdentifier?.Identifier));
         ViewActivitiesCommand = new AsyncRelayCommand(() => ShowActivitiesAsync(Account.ItemId));
      }

      public ICommand AddIdentifierCommand { get; }
      public ICommand MoveIdentifierUpCommand { get; }
      public ICommand MoveIdentifierDownCommand { get; }
      public ICommand DeleteIdentifierCommand { get; }
      public ICommand CopyIdentifierCommand { get; }
      public ICommand ShowQrCodeIdentifierCommand { get; }
      public ICommand ViewActivitiesCommand { get; }

      public IdentifierViewModel? SelectedIdentifier
      {
         get;
         set => SetProperty(ref field, value);
      }

      public string AccountDisplay
      {
         get
         {
            string accountDisplay = $"{Account.Label} {Account.Identifiers.First().Value}";
            return $"{(Account.HasChanged() ? "* " : string.Empty)}{accountDisplay.Trim()}";
         }
      }

      public string AccountId => Strings.Format(nameof(Strings.Msg_AccountId), Account.ItemId);

      public FieldVisualState LabelState => FieldVisualStateHelper.FromChanged(Account.HasChanged(nameof(Label)));
      public string Label
      {
         get => Account.Label;
         set
         {
            if (Account.Label != value)
            {
               Account.Label = value;
               _notify(nameof(Label));
            }
         }
      }

      public ObservableCollection<IdentifierViewModel> Identifiers = [];

      public FieldVisualState PasswordState
         => FieldVisualStateHelper.ForEditedSecret(
            isDirty: Account.HasChanged(nameof(Password)),
            isNotifiedWeak: PasswordWeak,
            isNotifiedLeak: PasswordLeaked);

      public string Password
      {
         get => Account.Password;
         set
         {
            if (Account.Password != value)
            {
               Account.Password = value;
               _notify(nameof(Password));
               AppServices.Session.Database?.RefreshAlerts();
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

      public FieldVisualState NotesState => FieldVisualStateHelper.FromChanged(Account.HasChanged(nameof(Notes)));
      public string Notes
      {
         get => Account.Notes;
         set
         {
            if (Account.Notes != value)
            {
               Account.Notes = value;
               _notify(nameof(Notes));
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
               AppServices.Session.Database?.RefreshAlerts();
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
               AppServices.Session.Database?.RefreshAlerts();
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

               OnPropertyChanged(nameof(WarnIfDuplicatedPassword));
               AppServices.Session.Database?.RefreshAlerts();
            }
         }
      }

      public bool WarnIfWeakPassword
      {
         get => Account.Options.HasFlag(AccountOption.WarnIfWeakPassword);
         set
         {
            if (WarnIfWeakPassword != value)
            {
               if (value)
               {
                  Account.Options |= AccountOption.WarnIfWeakPassword;
               }
               else
               {
                  Account.Options &= ~AccountOption.WarnIfWeakPassword;
               }

               OnPropertyChanged(nameof(WarnIfWeakPassword));
               AppServices.Session.Database?.RefreshAlerts();
            }
         }
      }

      public bool PasswordLeaked
         => Account.Options.HasFlag(AccountOption.WarnIfPasswordLeaked)
               && AppServices.Session.Alerts
                  .GetNotifiedAlerts(AlertKinds.PasswordLeaked)
                  .OfType<IAccountsAlert>()
                  .Any(x => x.Accounts.Contains(Account));

      public bool PasswordWeak
         => Account.Options.HasFlag(AccountOption.WarnIfWeakPassword)
               && AppServices.Session.Alerts
                  .GetNotifiedAlerts(AlertKinds.WeakAccountPassword)
                  .OfType<IAccountsAlert>()
                  .Any(x => x.Accounts.Contains(Account));

      public static IIdentifier[] IdentifierAutoCompleteList => AppServices.Session.User?.Services
         .SelectMany(x => x.Accounts)
         .SelectMany(x => x.Identifiers)
         .Where(x => !string.IsNullOrEmpty(x.Value))
         .OrderBy(x => x.Value)
         .ToArray() ?? [];

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         AppServices.Session.Alerts.NotifiedAlertsChanged -= _onAlertsChanged;
      }

      /// <summary>
      /// Re-reads account fields after an external model change (Undo/Redo).
      /// Clears identifier VMs so <c>AccountView</c> rebuilds rows from the model.
      /// </summary>
      public void SyncFromModel()
      {
         foreach (IdentifierViewModel identifier in Identifiers)
         {
            identifier.PropertyChanged -= _identifierViewModel_PropertyChanged;
         }

         Identifiers.Clear();
         OnPropertyChanged(string.Empty);
      }

      public void OnLanguageChanged()
      {
         OnPropertyChanged(nameof(AccountId));

         foreach (IdentifierViewModel identifier in Identifiers)
         {
            identifier.OnLanguageChanged();
         }
      }
      public void OnThemeChanged()
      {
         OnPropertyChanged(nameof(LabelState));
         OnPropertyChanged(nameof(PasswordState));
         OnPropertyChanged(nameof(NotesState));

         foreach (IdentifierViewModel identifier in Identifiers)
         {
            identifier.OnThemeChanged();
         }
      }

      private void _onAlertsChanged(object? sender, EventArgs e)
         => UiThread.Post(() =>
         {
            OnPropertyChanged(nameof(PasswordLeaked));
            OnPropertyChanged(nameof(PasswordState));
         });

      private void _notify(string propertyName)
      {
         OnPropertyChanged(propertyName);
         OnPropertyChanged($"{propertyName}State");
         OnPropertyChanged(nameof(AccountDisplay));
      }

      private void _identifierViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName is not (nameof(IdentifierViewModel.Identifier) or nameof(IdentifierViewModel.Type)))
         {
            return;
         }

         Account.Identifiers = [.. Identifiers.Select(x => x.ToIdentifier())];

         foreach (IdentifierViewModel identifier in Identifiers)
         {
            identifier.Refresh();
         }

         _notify(string.Empty);
      }

      public void AddIdentifier(IdentifierViewModel identifierViewModel)
      {
         identifierViewModel.PropertyChanged += _identifierViewModel_PropertyChanged;

         _identifierViewModel_PropertyChanged(null, new("Identifier"));
      }

      public IdentifierViewModel AddIdentifier(string identifier)
      {
         IdentifierViewModel identifierViewModel = new(Account, identifier);
         identifierViewModel.PropertyChanged += _identifierViewModel_PropertyChanged;

         Identifiers.Add(identifierViewModel);

         _identifierViewModel_PropertyChanged(null, new("Identifier"));

         return identifierViewModel;
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

      private async Task _addIdentifierAsync()
      {
         IdentifierViewModel added = AddIdentifier(string.Empty);
         SelectedIdentifier = added;
         await EditIdentifierAsync(added).ConfigureAwait(true);
      }

      /// <summary>Opens the insert-identifier picker for <paramref name="identifier"/> and applies a non-empty pick.</summary>
      public static async Task EditIdentifierAsync(IdentifierViewModel identifier)
      {
         ArgumentNullException.ThrowIfNull(identifier);
         if (AppServices.Dialogs is not { } dialogs)
         {
            return;
         }

         IIdentifier? picked = await dialogs
            .ShowAsync(new InsertIdentifierRequest(IdentifierAutoCompleteList, identifier.ToIdentifier()))
            .ConfigureAwait(true);

         if (!string.IsNullOrEmpty(picked?.Value))
         {
            identifier.Type = picked.Type;
            identifier.Identifier = picked.Value;
         }
      }

      /// <summary>Opens the activity log filtered on <paramref name="itemId"/>.</summary>
      internal static async Task ShowActivitiesAsync(string itemId)
      {
         if (AppServices.Dialogs is { } dialogs)
         {
            _ = await dialogs.ShowAsync(new ActivitiesRequest(NeedsReview: false, itemId)).ConfigureAwait(true);
         }
      }

      private void _moveIdentifierUp()
      {
         if (SelectedIdentifier is null)
         {
            return;
         }

         int index = Identifiers.IndexOf(SelectedIdentifier);
         if (MoveIdentifier(index, index - 1))
         {
            SelectedIdentifier = Identifiers[index - 1];
         }
      }

      private void _moveIdentifierDown()
      {
         if (SelectedIdentifier is null)
         {
            return;
         }

         int index = Identifiers.IndexOf(SelectedIdentifier);
         if (MoveIdentifier(index, index + 1))
         {
            SelectedIdentifier = Identifiers[index + 1];
         }
      }

      private void _deleteIdentifier()
      {
         if (SelectedIdentifier is null)
         {
            return;
         }

         int index = Identifiers.IndexOf(SelectedIdentifier);
         if (RemoveIdentifier(SelectedIdentifier))
         {
            SelectedIdentifier = Identifiers.Count == 0
               ? null
               : Identifiers[index < Identifiers.Count ? index : Identifiers.Count - 1];
         }
      }

      private void _copyIdentifier()
      {
         if (SelectedIdentifier is null)
         {
            return;
         }

         AppServices.Clipboard?.SetText(SelectedIdentifier.Identifier, ClipboardPolicy.AutoClearAfter);
      }

      public override string ToString() => $"{(Account.HasChanged() ? "* " : string.Empty)}{Account}";
   }
}
