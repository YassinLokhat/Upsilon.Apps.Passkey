using System.Windows;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Activities;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for UserLogsView.xaml
   /// </summary>
   internal sealed partial class UserActivitiesView : Window, ILanguageAware
   {
      internal readonly UserActivitiesViewModel ViewModel;

      internal UserActivitiesView(bool needsReviewFilter)
      {
         InitializeComponent();

         DataContext = ViewModel = new()
         {
            NeedsReview = needsReviewFilter,
         };

         _bindEventTypeCombo();

         _activities_DGV.ItemsSource = ViewModel.Activities;

         Loaded += (s, e) => this.PostLoadSetup();
      }

      public void OnLanguageChanged()
      {
         ViewModel.OnLanguageChanged();
         _bindEventTypeCombo();
      }

      private void _bindEventTypeCombo()
      {
         string selected = ViewModel.ReadableEventType;
         _eventType_CB.ItemsSource = ViewModel.EventTypeOptions;
         _eventType_CB.SelectedItem = selected;
      }
   }
}
