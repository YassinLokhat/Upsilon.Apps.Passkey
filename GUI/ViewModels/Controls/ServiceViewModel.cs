using System.ComponentModel;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   internal class ServiceViewModel(IService service) : INotifyPropertyChanged
   {
      private readonly IService _service = service;

      public string ServiceId => $"Service Id : {_service.ItemId.Replace(_service.User.ItemId, string.Empty)}";

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
      }
   }
}
