using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal class ServiceViewModel : INotifyPropertyChanged
   {
      public readonly IService Service;

      public string ServiceDisplay => $"{(Service.HasChanged() ? "* " : string.Empty)}{Service.ServiceName}";

      public string ServiceId => $"Service Id : {Service.ItemId.Replace(Service.User.ItemId, string.Empty)}";

      public Brush ServiceNameBackground => Service.HasChanged(nameof(ServiceName)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string ServiceName
      {
         get => Service.ServiceName;
         set
         {
            if (Service.ServiceName != value)
            {
               Service.ServiceName = value;
               OnPropertyChanged(nameof(ServiceName));
            }
         }
      }

      public Brush UrlBackground => Service.HasChanged(nameof(Url)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Url
      {
         get => Service.Url;
         set
         {
            if (Service.Url != value)
            {
               Service.Url = value;
               OnPropertyChanged(nameof(Url));
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
               OnPropertyChanged(nameof(Notes));
            }
         }
      }

      public ObservableCollection<AccountViewModel> Accounts = [];

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{propertyName}Background"));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServiceDisplay)));
      }

      public ServiceViewModel(IService service, string identifierFilter = "", string textFilter = "", bool changedItemsOnly = false)
      {
         Service = service;

         IAccount[] accounts = [.. Service.Accounts.Where(x => x.MeetsFilterConditions(identifierFilter, textFilter, changedItemsOnly))];

         if (accounts.Length == 0)
         {
            accounts = Service.Accounts;
         }

         foreach (IAccount account in accounts)
         {
            AccountViewModel accountViewModel = new(account);
            accountViewModel.PropertyChanged += _accountViewModel_PropertyChanged;
            Accounts.Add(accountViewModel);
         }
      }

      public AccountViewModel AddAccount()
      {
         AccountViewModel? accountViewModel = Accounts.FirstOrDefault(x => x.Identifiers.Any(y => y.Identifier == "NewAccount"));

         if (accountViewModel is null)
         {
            accountViewModel = new(Service.AddAccount(["NewAccount"]));
            accountViewModel.PropertyChanged += _accountViewModel_PropertyChanged;
            Accounts.Insert(0, accountViewModel);

            OnPropertyChanged(string.Empty);
         }

         return accountViewModel;
      }

      public int DeleteAccount(AccountViewModel accountViewModel)
      {
         int index = Accounts.IndexOf(accountViewModel);

         _ = Accounts.Remove(accountViewModel);
         Service.DeleteAccount(accountViewModel.Account);

         OnPropertyChanged(string.Empty);

         return index < Accounts.Count ? index : Accounts.Count - 1;
      }

      private void _accountViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         OnPropertyChanged(string.Empty);
      }

      public override string ToString() => $"{(Service.HasChanged() ? "* " : string.Empty)}{Service}";
   }
}
