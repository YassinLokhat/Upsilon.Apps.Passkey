using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.Core.Public.Utils;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
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

      public ServiceViewModel(IService service)
      {
         Service = service;

         foreach (IAccount account in Service.Accounts)
         {
            AccountViewModel accountViewModel = new(account);
            accountViewModel.PropertyChanged += _accountViewModel_PropertyChanged;
            Accounts.Add(accountViewModel);
         }
      }

      private void _accountViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         OnPropertyChanged(string.Empty);
      }

      internal bool MeetFilterConditions(string serviceFilter, string identifiantFilter, string textFilter)
         => _matchServiceFilter(serviceFilter.ToLower())
            && _matchIdentifiantFilter(identifiantFilter.ToLower())
            && _matchTextFilter(textFilter.ToLower());

      private bool _matchServiceFilter(string serviceFilter)
      {
         if (string.IsNullOrWhiteSpace(serviceFilter)) return true;

         string serviceId = Service.ItemId.ToLower();
         string serviceName = Service.ServiceName.ToLower();

         return serviceId.StartsWith(serviceFilter)
            || serviceName.Contains(serviceFilter);
      }

      private bool _matchIdentifiantFilter(string identifiantFilter)
      {
         if (string.IsNullOrWhiteSpace(identifiantFilter)) return true;

         string serviceId = Service.ItemId.ToLower();
         string serviceName = Service.ServiceName.ToLower();

         return serviceId.StartsWith(identifiantFilter)
            || serviceName.Contains(identifiantFilter);
      }

      private bool _matchTextFilter(string textFilter)
      {
         if (string.IsNullOrWhiteSpace(textFilter)) return true;

         string serviceId = Service.ItemId.ToLower();
         string serviceName = Service.ServiceName.ToLower();
         string serviceUrl = Service.Url.ToLower();
         string serviceNote = Service.Notes.ToLower();

         return serviceId.Contains(textFilter)
            || serviceName.Contains(textFilter)
            || serviceUrl.Contains(textFilter)
            || serviceNote.Contains(textFilter);
      }

      public override string ToString() => $"{(Service.HasChanged() ? "* " : string.Empty)}{Service}";
   }
}
