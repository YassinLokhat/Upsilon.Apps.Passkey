using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Vault
{
   internal sealed class ServiceViewModel : ObservableObject
   {
      public readonly IService Service;

      private readonly Dictionary<string, AccountViewModel> _accountViewModelsById = new(StringComparer.Ordinal);

      public string ServiceDisplay => $"{(Service.HasChanged() ? "* " : string.Empty)}{Service.ServiceName}";

      public string ServiceId => Strings.Format(nameof(Strings.Msg_ServiceId), Service.ItemId);

      public AccountViewModel? SelectedAccount
      {
         get;
         set => SetProperty(ref field, value);
      }

      public ICommand AddAccountCommand { get; }
      public ICommand DeleteAccountCommand { get; }
      public ICommand OpenUrlCommand { get; }
      public ICommand ViewActivitiesCommand { get; }
      public ICommand MoveAccountUpCommand { get; }
      public ICommand MoveAccountDownCommand { get; }

      public FieldVisualState ServiceNameState => FieldVisualStateHelper.FromChanged(Service.HasChanged(nameof(ServiceName)));
      public string ServiceName
      {
         get => Service.ServiceName;
         set
         {
            if (Service.ServiceName != value)
            {
               Service.ServiceName = value;
               _notify(nameof(ServiceName));
            }
         }
      }

      public FieldVisualState UrlState => FieldVisualStateHelper.FromChanged(Service.HasChanged(nameof(Url)));
      public string Url
      {
         get => Service.Url?.OriginalString ?? string.Empty;
         set
         {
            if (Service.Url?.OriginalString == value)
            {
               return;
            }

            // Empty clears. Absolute non-http(s) schemes are refused. Incomplete
            // relative values are kept so PropertyChanged typing still works;
            // OpenUrl re-validates with the http(s) allowlist.
            if (string.IsNullOrWhiteSpace(value))
            {
               Service.Url = null;
            }
            else if (Uri.TryCreate(value, UriKind.Absolute, out Uri? absolute))
            {
               if (!ServiceUrlHelper.IsAllowedScheme(absolute))
               {
                  return;
               }

               Service.Url = absolute;
            }
            else if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri? partial))
            {
               Service.Url = partial;
            }
            else
            {
               return;
            }

            _notify(nameof(Url));
         }
      }

      public FieldVisualState NotesState => FieldVisualStateHelper.FromChanged(Service.HasChanged(nameof(Notes)));
      public string Notes
      {
         get => Service.Notes;
         set
         {
            if (Service.Notes != value)
            {
               Service.Notes = value;
               _notify(nameof(Notes));
            }
         }
      }

      public ObservableCollection<AccountViewModel> Accounts { get; } = [];

      private void _notify(string propertyName)
      {
         OnPropertyChanged(propertyName);
         OnPropertyChanged($"{propertyName}State");
         OnPropertyChanged(nameof(ServiceDisplay));
      }

      public ServiceViewModel(IService service)
      {
         Service = service;
         _syncAccountViewModels();

         AddAccountCommand = new AsyncRelayCommand(_addAccountAsync);
         DeleteAccountCommand = new AsyncRelayCommand(_deleteAccountAsync);
         OpenUrlCommand = new AsyncRelayCommand(_openUrlAsync);
         ViewActivitiesCommand = new AsyncRelayCommand(() => AccountViewModel.ShowActivitiesAsync(Service.ItemId));
         MoveAccountUpCommand = new RelayCommand(_moveAccountUp);
         MoveAccountDownCommand = new RelayCommand(_moveAccountDown);
      }

      public void OnLanguageChanged()
      {
         OnPropertyChanged(nameof(ServiceId));
         foreach (AccountViewModel account in _accountViewModelsById.Values)
         {
            account.OnLanguageChanged();
         }
      }

      public void OnThemeChanged()
      {
         OnPropertyChanged(nameof(ServiceNameState));
         OnPropertyChanged(nameof(UrlState));
         OnPropertyChanged(nameof(NotesState));

         foreach (AccountViewModel account in _accountViewModelsById.Values)
         {
            account.OnThemeChanged();
         }
      }

      /// <summary>
      /// Re-reads service/account fields after an external model change (Undo/Redo).
      /// </summary>
      public void SyncFromModel()
      {
         _syncAccountViewModels();

         foreach (AccountViewModel account in _accountViewModelsById.Values)
         {
            account.SyncFromModel();
         }

         _notify(string.Empty);
      }

      /// <summary>
      /// Applies list filters to visible accounts. With a concrete type filter, an empty match stays empty;
      /// with All, a service-level text hit with no account match still shows every account.
      /// </summary>
      public void ApplyFilters(string identifierFilter, string textFilter, bool changedItemsOnly, IdentifierType identifierTypeFilter)
      {
         _syncAccountViewModels();

         IAccount[] matching = [.. Service.Accounts.Where(x => x.MeetsFilterConditions(identifierFilter, textFilter, changedItemsOnly, identifierTypeFilter))];
         // Preserve prior behavior for text-only global hits on the service: if no account
         // matched, still list all accounts. When a type filter is active, keep the empty set.
         IAccount[] toShow = matching.Length != 0 || identifierTypeFilter != IdentifierViewModel.AllIdentifierType
            ? matching
            : [.. Service.Accounts];
         HashSet<string> visibleIds = [.. toShow.Select(x => x.ItemId)];

         string? selectedId = SelectedAccount?.Account.ItemId;

         Accounts.Clear();

         foreach (IAccount account in Service.Accounts.Where(x => visibleIds.Contains(x.ItemId)))
         {
            Accounts.Add(_accountViewModelsById[account.ItemId]);
         }

         SelectedAccount = selectedId is not null
            ? Accounts.FirstOrDefault(x => x.Account.ItemId == selectedId) ?? Accounts.FirstOrDefault()
            : Accounts.FirstOrDefault();
      }

      public async Task<AccountViewModel?> AddAccountAsync()
      {
         IIdentifier? identifier = AppServices.Dialogs is { } dialogs
            ? await dialogs.ShowAsync(new InsertIdentifierRequest(AccountViewModel.IdentifierAutoCompleteList, null)).ConfigureAwait(true)
            : null;

         if (identifier is null)
         {
            return null;
         }

         IAccount account = Service.AddAccount(
         [
            new Identifier(identifier),
         ]);

         _syncAccountViewModels();
         AccountViewModel accountViewModel = _accountViewModelsById[account.ItemId];

         if (!Accounts.Contains(accountViewModel))
         {
            Accounts.Insert(0, accountViewModel);
         }

         _notify(string.Empty);
         AppServices.Session.Database?.RefreshAlerts();

         return accountViewModel;
      }

      public int DeleteAccount(AccountViewModel accountViewModel)
      {
         int index = Accounts.IndexOf(accountViewModel);

         _ = Accounts.Remove(accountViewModel);
         Service.DeleteAccount(accountViewModel.Account);
         _removeAccountViewModel(accountViewModel);

         _notify(string.Empty);
         AppServices.Session.Database?.RefreshAlerts();

         return index < Accounts.Count ? index : Accounts.Count - 1;
      }

      private void _syncAccountViewModels()
      {
         HashSet<string> liveIds = [.. Service.Accounts.Select(x => x.ItemId)];

         foreach (string id in _accountViewModelsById.Keys.Where(k => !liveIds.Contains(k)).ToList())
         {
            _removeAccountViewModel(_accountViewModelsById[id]);
         }

         foreach (IAccount account in Service.Accounts)
         {
            if (_accountViewModelsById.ContainsKey(account.ItemId))
            {
               continue;
            }

            AccountViewModel accountViewModel = new(account);
            accountViewModel.PropertyChanged += _accountViewModel_PropertyChanged;
            _accountViewModelsById[account.ItemId] = accountViewModel;
         }
      }

      private void _removeAccountViewModel(AccountViewModel accountViewModel)
      {
         accountViewModel.PropertyChanged -= _accountViewModel_PropertyChanged;
         accountViewModel.Dispose();
         _ = _accountViewModelsById.Remove(accountViewModel.Account.ItemId);
      }

      private void _accountViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         _notify(string.Empty);
      }

      private static IDialogService _dialogs
         => AppServices.Dialogs ?? throw new InvalidOperationException("No dialog service registered.");

      private async Task _addAccountAsync()
      {
         AccountViewModel? accountViewModel = await AddAccountAsync().ConfigureAwait(true);

         if (accountViewModel is not null)
         {
            SelectedAccount = accountViewModel;
         }
      }

      private async Task _deleteAccountAsync()
      {
         if (SelectedAccount is not { } accountViewModel
            || await _dialogs.ConfirmAsync(
               Strings.Format(nameof(Strings.Msg_DeleteAccount), accountViewModel.AccountDisplay),
               Strings.Title_DeleteAccount).ConfigureAwait(true) != DialogResult.Yes)
         {
            return;
         }

         int index = DeleteAccount(accountViewModel);
         SelectedAccount = index >= 0 && index < Accounts.Count ? Accounts[index] : null;
      }

      private async Task _openUrlAsync()
      {
         if (string.IsNullOrWhiteSpace(Url))
         {
            return;
         }

         switch (ServiceUrlHelper.ClassifyForOpen(Url))
         {
            case ServiceUrlHelper.OpenDisposition.Rejected:
               await _dialogs.ErrorAsync(Strings.Msg_UrlSchemeNotAllowed, Strings.Title_OpenUrl).ConfigureAwait(true);
               return;

            case ServiceUrlHelper.OpenDisposition.RequiresHttpConfirmation:
               if (await _dialogs.ConfirmAsync(
                     Strings.Format(nameof(Strings.Msg_OpenHttpUrlConfirm), Url),
                     Strings.Title_OpenUrl,
                     DialogButtons.YesNo,
                     DialogIcon.Warning).ConfigureAwait(true)
                  != DialogResult.Yes)
               {
                  return;
               }

               break;

            case ServiceUrlHelper.OpenDisposition.OpenDirect:
               break;
         }

         if (!ServiceUrlHelper.TryCreateAllowedUri(Url, out Uri? uri) || uri is null)
         {
            return;
         }

         if (AppServices.Launcher is { } launcher)
         {
            await launcher.OpenUriAsync(uri).ConfigureAwait(true);
         }
      }

      private void _moveAccountUp()
      {
         if (SelectedAccount is null)
         {
            return;
         }

         int index = Accounts.IndexOf(SelectedAccount);
         if (MoveAccount(index, index - 1))
         {
            SelectedAccount = Accounts[index - 1];
         }
      }

      private void _moveAccountDown()
      {
         if (SelectedAccount is null)
         {
            return;
         }

         int index = Accounts.IndexOf(SelectedAccount);
         if (MoveAccount(index, index + 1))
         {
            SelectedAccount = Accounts[index + 1];
         }
      }

      public bool MoveAccount(int oldIndex, int newIndex)
      {
         if (oldIndex < 0
            || newIndex < 0
            || newIndex >= Accounts.Count)
         {
            return false;
         }

         (Accounts[newIndex], Accounts[oldIndex]) = (Accounts[oldIndex], Accounts[newIndex]);

         Service.Accounts = [.. Accounts.Select(x => x.Account)];

         _notify("Account");

         return true;
      }

      public override string ToString() => $"{(Service.HasChanged() ? "* " : string.Empty)}{Service}";
   }
}
