using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.Views;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class AccountViewModel : ObservableObject, IThemeAware, IDisposable
   {
      public readonly IAccount Account;
      private bool _disposed;

      public AccountViewModel(IAccount account)
      {
         Account = account;
         AppServices.Session.Alerts.NotifiedAlertsChanged += _onAlertsChanged;

         AddIdentifierCommand = new RelayCommand(_addIdentifier);
         MoveIdentifierUpCommand = new RelayCommand(_moveIdentifierUp);
         MoveIdentifierDownCommand = new RelayCommand(_moveIdentifierDown);
         DeleteIdentifierCommand = new RelayCommand(_deleteIdentifier);
         CopyIdentifierCommand = new RelayCommand(_copyIdentifier);
         ShowQrCodeIdentifierCommand = new RelayCommand(_showQrCodeIdentifier);
         ViewActivitiesCommand = new RelayCommand(_viewActivities);
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

      public Brush LabelBackground => Account.HasChanged(nameof(Label)) ? FieldStateBrushes.ChangedBrush : FieldStateBrushes.UnchangedBrush2;
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

      public Brush PasswordBackground
         => SecretFieldBrushes.Background(
            isDirty: Account.HasChanged(nameof(Password)),
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

      public Brush NotesBackground => Account.HasChanged(nameof(Notes)) ? FieldStateBrushes.ChangedBrush : FieldStateBrushes.UnchangedBrush2;
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

      public bool PasswordLeaked
         => Account.Options.HasFlag(AccountOption.WarnIfPasswordLeaked)
               && AppServices.Session.Alerts
                  .GetNotifiedAlerts(AlertKinds.PasswordLeaked)
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
         OnPropertyChanged(nameof(LabelBackground));
         OnPropertyChanged(nameof(PasswordBackground));
         OnPropertyChanged(nameof(NotesBackground));

         foreach (IdentifierViewModel identifier in Identifiers)
         {
            identifier.OnThemeChanged();
         }
      }

      private void _onAlertsChanged(object? sender, EventArgs e)
         => UiThread.Post(() =>
         {
            OnPropertyChanged(nameof(PasswordLeaked));
            OnPropertyChanged(nameof(PasswordBackground));
         });

      private void _notify(string propertyName)
      {
         OnPropertyChanged(propertyName);
         OnPropertyChanged($"{propertyName}Background");
         OnPropertyChanged(nameof(AccountDisplay));
      }

      private void _identifierViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName is not (nameof(IdentifierViewModel.Identifier) or nameof(IdentifierViewModel.Type)))
         {
            return;
         }

         Account.Identifiers = [.. Identifiers.Select(x => x.ToIdentifier())];

         foreach (IdentifierViewModel? identifier in Identifiers.Except([sender]).Cast<IdentifierViewModel?>())
         {
            identifier?.Refresh();
         }

         _notify(string.Empty);
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

      private void _addIdentifier()
      {
         AddIdentifier(string.Empty);
         SelectedIdentifier = Identifiers.LastOrDefault();
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

         AppServices.Clipboard.SetText(SelectedIdentifier.Identifier, ClipboardManager.AutoClearAfter);
      }

      private void _showQrCodeIdentifier()
      {
         if (SelectedIdentifier is null)
         {
            return;
         }

         QrCodeView.ShowQrCode(null,
            SelectedIdentifier.Identifier,
            AppServices.Session.User?.Settings.ShowPasswordDelay ?? 0);
      }

      private void _viewActivities()
      {
         string itemId = Account.ItemId;

         _ = AppServices.Dialogs.ShowSingleton(
            factory: () =>
            {
               UserActivitiesView view = new(needsReviewFilter: false);
               view.ViewModel.ClearFilters();
               view.ViewModel.SearchCriteria = itemId;
               return view;
            },
            configure: view => view.ViewModel.SearchCriteria = itemId);
      }

      public override string ToString() => $"{(Account.HasChanged() ? "* " : string.Empty)}{Account}";
   }
}
