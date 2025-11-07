using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.Core.Public.Utils;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   internal class ServiceViewModel(IService service) : INotifyPropertyChanged
   {
      private readonly IService _service = service;

      private static Brush _unchangedBrush => new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
      private static Brush _changedBrush => new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60));

      public string ServiceId => $"Service Id : {_service.ItemId.Replace(_service.User.ItemId, string.Empty)}";

      public Brush ServiceNameBackground => _service.HasChanged(nameof(ServiceName)) ? _changedBrush : _unchangedBrush;
      public string ServiceName
      {
         get => _service.ServiceName;
         set
         {
            if (_service.ServiceName != value)
            {
               _service.ServiceName = value;
               OnPropertyChanged(nameof(ServiceName));
               OnPropertyChanged(nameof(ServiceNameBackground));
            }
         }
      }

      public Brush UrlBackground => _service.HasChanged(nameof(Url)) ? _changedBrush : _unchangedBrush;
      public string Url
      {
         get => _service.Url;
         set
         {
            if (_service.Url != value)
            {
               _service.Url = value;
               OnPropertyChanged(nameof(Url));
               OnPropertyChanged(nameof(UrlBackground));
            }
         }
      }

      public Brush NotesBackground => _service.HasChanged(nameof(Notes)) ? _changedBrush : _unchangedBrush;
      public string Notes
      {
         get => _service.Notes;
         set
         {
            if (_service.Notes != value)
            {
               _service.Notes = value;
               OnPropertyChanged(nameof(Notes));
               OnPropertyChanged(nameof(NotesBackground));
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
