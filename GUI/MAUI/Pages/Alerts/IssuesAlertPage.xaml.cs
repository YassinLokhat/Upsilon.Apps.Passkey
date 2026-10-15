using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Alerts
{
   /// <summary>
   /// Passkey-quality or security-settings issues. Settings shortcuts arrive with
   /// the MAUI settings pages; until then the page only lists the issues.
   /// </summary>
   internal sealed partial class IssuesAlertPage : ClosablePage
   {
      private readonly IDisposable _viewModel;

      private IssuesAlertPage(IDisposable viewModel, string title, Func<IssueItemViewModel[]> issues)
      {
         InitializeComponent();
         _viewModel = viewModel;
         Title = title;
         IssuesList.ItemsSource = issues();

         if (viewModel is INotifyPropertyChanged observable)
         {
            observable.PropertyChanged += (s, e) =>
            {
               if (e.PropertyName == nameof(PasskeyQualityAlertViewModel.Issues))
               {
                  IssuesList.ItemsSource = issues();
               }
            };
         }
      }

      public static IssuesAlertPage ForPasskeyQuality()
      {
         PasskeyQualityAlertViewModel vm = new();
         return new IssuesAlertPage(vm, vm.Title, () => vm.Issues);
      }

      public static IssuesAlertPage ForSecuritySettings()
      {
         SecuritySettingsAlertViewModel vm = new();
         return new IssuesAlertPage(vm, vm.Title, () => vm.Issues);
      }

      protected override void OnCompleted() => _viewModel.Dispose();

      private void _onClose(object? sender, EventArgs e) => CompletePage();
   }
}
