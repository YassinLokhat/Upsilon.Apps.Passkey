using System.ComponentModel;
using System.Windows.Media;
using Upsilon.Apps.Passkey.Core.Public.Utils;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   internal class ServiceViewModel(IService service) : INotifyPropertyChanged
   {
      private readonly IService _service = service;

      private static Brush _unchangedBrush1 => new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
      private static Brush _unchangedBrush2 => new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
      private static Brush _changedBrush => new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60));

      public string ServiceDisplay => $"{(_service.HasChanged() ? "* " : string.Empty)}{_service}";
      public Brush ServiceDisplayBackground => _service.HasChanged() ? _changedBrush : _unchangedBrush1;

      public string ServiceId => $"Service Id : {_service.ItemId.Replace(_service.User.ItemId, string.Empty)}";

      public Brush ServiceNameBackground => _service.HasChanged(nameof(ServiceName)) ? _changedBrush : _unchangedBrush2;
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

      public Brush UrlBackground => _service.HasChanged(nameof(Url)) ? _changedBrush : _unchangedBrush2;
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

      public Brush NotesBackground => _service.HasChanged(nameof(Notes)) ? _changedBrush : _unchangedBrush2;
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
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServiceDisplayBackground)));
      }
   }
}
