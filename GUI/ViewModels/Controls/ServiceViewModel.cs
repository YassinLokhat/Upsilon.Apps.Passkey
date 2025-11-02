using System.ComponentModel;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   internal class ServiceViewModel : INotifyPropertyChanged
   {
      private readonly IService _service;

      public string ServiceId => $"Service Id : {_service.ItemId}";

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

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public ServiceViewModel(IService service)
      {
         _service = service;
      }
   }
}
