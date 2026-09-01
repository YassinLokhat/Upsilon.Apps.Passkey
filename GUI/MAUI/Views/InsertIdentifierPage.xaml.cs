using Upsilon.Apps.Passkey.GUI.MAUI.Services;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
   [QueryProperty(nameof(InitialText), "Initial")]
   public partial class InsertIdentifierPage : ContentPage
   {
      private InsertIdentifierViewModel? _viewModel;

      public InsertIdentifierPage()
      {
         InitializeComponent();
      }

      public string InitialText
      {
         get;
         set
         {
            field = value;
            IEnumerable<string> known = AppServices.Session.User?.Services
               .SelectMany(s => s.Accounts)
               .SelectMany(a => a.Identifiers) ?? [];
            _viewModel = new InsertIdentifierViewModel(known, value);
            BindingContext = _viewModel;
         }
      } = string.Empty;

      protected override void OnAppearing()
      {
         base.OnAppearing();
      }
   }
}
