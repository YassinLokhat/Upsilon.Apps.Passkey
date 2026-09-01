using Upsilon.Apps.Passkey.GUI.MAUI.Services;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
   [QueryProperty(nameof(ContentText), "Content")]
   public partial class QrCodePage : ContentPage
   {
      private readonly QrCodeViewModel _viewModel = new();

      public QrCodePage()
      {
         InitializeComponent();
         BindingContext = _viewModel;
      }

      public string ContentText
      {
         get;
         set
         {
            field = value;
            int delay = AppServices.Session.User?.Settings.ShowPasswordDelay ?? 0;
            _viewModel.Load(value ?? string.Empty, delay);
         }
      } = string.Empty;

      protected override void OnDisappearing()
      {
         _viewModel.Unload();
         base.OnDisappearing();
      }
   }
}
