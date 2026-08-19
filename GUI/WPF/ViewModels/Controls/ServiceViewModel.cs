using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Principal;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class ServiceViewModel : INotifyPropertyChanged
   {
      public readonly IService Service;

      private readonly Dictionary<string, AccountViewModel> _accountViewModelsById = new(StringComparer.Ordinal);

      public string ServiceDisplay => $"{(Service.HasChanged() ? "* " : string.Empty)}{Service.ServiceName}";

      public string ServiceId => $"Service Id : {Service.ItemId}";

      public Brush ServiceNameBackground => Service.HasChanged(nameof(ServiceName)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string ServiceName
      {
         get => Service.ServiceName;
         set
         {
            if (Service.ServiceName != value)
            {
               Service.ServiceName = value;
               _onPropertyChanged(nameof(ServiceName));
            }
         }
      }

      public Brush UrlBackground => Service.HasChanged(nameof(Url)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Url
      {
         get => Service.Url?.OriginalString ?? string.Empty;
         set
         {
            if (Service.Url?.OriginalString != value)
            {
               Service.Url = new(value);
               _onPropertyChanged(nameof(Url));
            }
         }
      }

      public Brush NotesBackground => Service.HasChanged(nameof(Notes)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Notes
      {
         get => Service.Notes;
         set
         {
            if (Service.Notes != value)
            {
               Service.Notes = value;
               _onPropertyChanged(nameof(Notes));
            }
         }
      }

      public ObservableCollection<AccountViewModel> Accounts = [];

      public event PropertyChangedEventHandler? PropertyChanged;

      private void _onPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{propertyName}Background"));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServiceDisplay)));
      }

      public ServiceViewModel(IService service)
      {
         Service = service;
         _syncAccountViewModels();
      }

      public void ApplyFilters(string identifierFilter, string textFilter, bool changedItemsOnly)
      {
         _syncAccountViewModels();

         IAccount[] matching = [.. Service.Accounts.Where(x => x.MeetsFilterConditions(identifierFilter, textFilter, changedItemsOnly))];
         IAccount[] toShow = matching.Length != 0 ? matching : [.. Service.Accounts];
         HashSet<string> visibleIds = [.. toShow.Select(x => x.ItemId)];

         Accounts.Clear();

         foreach (IAccount account in Service.Accounts.Where(x => visibleIds.Contains(x.ItemId)))
         {
            Accounts.Add(_accountViewModelsById[account.ItemId]);
         }
      }

      public AccountViewModel AddAccount()
      {
         AccountViewModel? accountViewModel = Accounts.FirstOrDefault(x => x.Identifiers.Any(y => y.Identifier.StartsWith("👤New Account #", StringComparison.Ordinal)))
            ?? _accountViewModelsById.Values.FirstOrDefault(x => x.Identifiers.Any(y => y.Identifier.StartsWith("👤New Account #", StringComparison.Ordinal)));

         if (accountViewModel is null)
         {
            IAccount account = Service.AddAccount(["👤New Account #" + DateTime.Now.Ticks]);
            _syncAccountViewModels();
            accountViewModel = _accountViewModelsById[account.ItemId];

            if (!Accounts.Contains(accountViewModel))
            {
               Accounts.Insert(0, accountViewModel);
            }

            _onPropertyChanged(string.Empty);
         }

         return accountViewModel;
      }

      public int DeleteAccount(AccountViewModel accountViewModel)
      {
         int index = Accounts.IndexOf(accountViewModel);

         _ = Accounts.Remove(accountViewModel);
         Service.DeleteAccount(accountViewModel.Account);
         _removeAccountViewModel(accountViewModel);

         _onPropertyChanged(string.Empty);

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
         _ = _accountViewModelsById.Remove(accountViewModel.Account.ItemId);
      }

      private void _accountViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         _onPropertyChanged(string.Empty);
      }

      public override string ToString() => $"{(Service.HasChanged() ? "* " : string.Empty)}{Service}";
   }
}
