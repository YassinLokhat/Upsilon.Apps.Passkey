using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.Views;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class ServiceViewModel : ObservableObject, IThemeAware
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

      public Brush ServiceNameBackground => Service.HasChanged(nameof(ServiceName)) ? FieldStateBrushes.ChangedBrush : FieldStateBrushes.UnchangedBrush2;
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

      public Brush UrlBackground => Service.HasChanged(nameof(Url)) ? FieldStateBrushes.ChangedBrush : FieldStateBrushes.UnchangedBrush2;
      public string Url
      {
         get => Service.Url?.OriginalString ?? string.Empty;
         set
         {
            if (Service.Url?.OriginalString != value)
            {
               Service.Url = new(value);
               _notify(nameof(Url));
            }
         }
      }

      public Brush NotesBackground => Service.HasChanged(nameof(Notes)) ? FieldStateBrushes.ChangedBrush : FieldStateBrushes.UnchangedBrush2;
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

      public readonly ObservableCollection<AccountViewModel> Accounts = [];

      private void _notify(string propertyName)
      {
         OnPropertyChanged(propertyName);
         OnPropertyChanged($"{propertyName}Background");
         OnPropertyChanged(nameof(ServiceDisplay));
      }

      public ServiceViewModel(IService service)
      {
         Service = service;
         _syncAccountViewModels();

         AddAccountCommand = new RelayCommand(_addAccount);
         DeleteAccountCommand = new RelayCommand(_deleteAccount);
         OpenUrlCommand = new RelayCommand(_openUrl);
         ViewActivitiesCommand = new RelayCommand(_viewActivities);
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
         OnPropertyChanged(nameof(ServiceNameBackground));
         OnPropertyChanged(nameof(UrlBackground));
         OnPropertyChanged(nameof(NotesBackground));

         foreach (AccountViewModel account in _accountViewModelsById.Values)
         {
            account.OnThemeChanged();
         }
      }

      public void ApplyFilters(string identifierFilter, string textFilter, bool changedItemsOnly)
      {
         _syncAccountViewModels();

         IAccount[] matching = [.. Service.Accounts.Where(x => x.MeetsFilterConditions(identifierFilter, textFilter, changedItemsOnly))];
         IAccount[] toShow = matching.Length != 0 ? matching : [.. Service.Accounts];
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

      public AccountViewModel AddAccount()
      {
         AccountViewModel? accountViewModel = Accounts.FirstOrDefault(_isNewAccountPlaceholder)
            ?? _accountViewModelsById.Values.FirstOrDefault(_isNewAccountPlaceholder);

         if (accountViewModel is null)
         {
            IAccount account = Service.AddAccount(
            [
               new Identifier(
                  IdentifierType.Username,
                  Strings.Msg_NewAccountPrefix + DateTime.Now.Ticks),
            ]);
            _syncAccountViewModels();
            accountViewModel = _accountViewModelsById[account.ItemId];

            if (!Accounts.Contains(accountViewModel))
            {
               Accounts.Insert(0, accountViewModel);
            }

            _notify(string.Empty);
            AppServices.Session.Database?.RefreshAlerts();
         }

         return accountViewModel;
      }

      private static bool _isNewAccountPlaceholder(AccountViewModel account)
         => account.Identifiers.Any(id =>
            Strings.IsPlaceholderName(id.Identifier, nameof(Strings.Msg_NewAccountPrefix)));

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

      private void _addAccount()
         => SelectedAccount = AddAccount();

      private void _deleteAccount()
      {
         if (SelectedAccount is not { } accountViewModel
            || AppServices.Dialogs.Confirm(
               Strings.Format(nameof(Strings.Msg_DeleteAccount), accountViewModel.AccountDisplay),
               Strings.Title_DeleteAccount) != MessageBoxResult.Yes)
         {
            return;
         }

         int index = DeleteAccount(accountViewModel);
         SelectedAccount = index >= 0 && index < Accounts.Count ? Accounts[index] : null;
      }

      private void _openUrl()
      {
         if (string.IsNullOrWhiteSpace(Url))
         {
            return;
         }

         using Process process = new()
         {
            StartInfo = new ProcessStartInfo(Url)
            {
               UseShellExecute = true,
            },
         };

         _ = process.Start();
      }

      private void _viewActivities()
      {
         string itemId = Service.ItemId;

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

      public override string ToString() => $"{(Service.HasChanged() ? "* " : string.Empty)}{Service}";
   }
}
