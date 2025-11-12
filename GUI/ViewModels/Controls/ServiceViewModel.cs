using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.Core.Public.Utils;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   internal class ServiceViewModel(IService service) : INotifyPropertyChanged
   {
      private readonly IService _service = service;

      public string ServiceDisplay => $"{(_service.HasChanged() ? "* " : string.Empty)}{_service}";

      public string ServiceId => $"Service Id : {_service.ItemId.Replace(_service.User.ItemId, string.Empty)}";

      public Brush ServiceNameBackground => _service.HasChanged(nameof(ServiceName)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string ServiceName
      {
         get => _service.ServiceName;
         set
         {
            if (_service.ServiceName != value)
            {
               _service.ServiceName = value;
               OnPropertyChanged(nameof(ServiceName));
            }
         }
      }

      public Brush UrlBackground => _service.HasChanged(nameof(Url)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Url
      {
         get => _service.Url;
         set
         {
            if (_service.Url != value)
            {
               _service.Url = value;
               OnPropertyChanged(nameof(Url));
            }
         }
      }

      public Brush NotesBackground => _service.HasChanged(nameof(Notes)) ? DarkMode.ChangedBrush : DarkMode.UnchangedBrush2;
      public string Notes
      {
         get => _service.Notes;
         set
         {
            if (_service.Notes != value)
            {
               _service.Notes = value;
               OnPropertyChanged(nameof(Notes));
            }
         }
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{propertyName}Background"));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServiceDisplay)));
      }

      internal bool MeetFilterConditions(string serviceFilter, string identifiantFilter, string textFilter)
         => _matchServiceFilter(serviceFilter.ToLower())
            && _matchIdentifiantFilter(identifiantFilter.ToLower())
            && _matchTextFilter(textFilter.ToLower());

      private bool _matchServiceFilter(string serviceFilter)
      {
         if (string.IsNullOrWhiteSpace(serviceFilter)) return true;

         string serviceId = _service.ItemId.ToLower();
         string serviceName = _service.ServiceName.ToLower();

         if (serviceId.StartsWith(serviceFilter)
            || serviceName.Contains(serviceFilter))
            return true;

         return false;
      }

      private bool _matchIdentifiantFilter(string identifiantFilter)
      {
         if (string.IsNullOrWhiteSpace(identifiantFilter)) return true;

         string serviceId = _service.ItemId.ToLower();
         string serviceName = _service.ServiceName.ToLower();

         if (serviceId.StartsWith(identifiantFilter)
            || serviceName.Contains(identifiantFilter))
            return true;

         return false;
      }

      private bool _matchTextFilter(string textFilter)
      {
         if (string.IsNullOrWhiteSpace(textFilter)) return true;

         string serviceId = _service.ItemId.ToLower();
         string serviceName = _service.ServiceName.ToLower();
         string serviceUrl = _service.Url.ToLower();
         string serviceNote = _service.Notes.ToLower();

         if (serviceId.Contains(textFilter)
            || serviceName.Contains(textFilter)
            || serviceUrl.Contains(textFilter)
            || serviceNote.Contains(textFilter))
            return true;

         return false;
      }

      public override string ToString() => $"{(_service.HasChanged() ? "* " : string.Empty)}{_service}";
   }
}
