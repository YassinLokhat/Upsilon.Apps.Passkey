using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Enums;

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
         => _bindEventTypeCombo();

      private void _bindEventTypeCombo()
      {
         ActivityEventType selected = ViewModel.EventType;
         _eventType_CB.ItemsSource = Enum.GetValues<ActivityEventType>()
            .Cast<ActivityEventType>()
            .Select(x => x.ToReadableString())
            .ToArray();
         _eventType_CB.SelectedItem = selected.ToReadableString();
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         AppServices.Navigation.RequestItem(ViewModel.Activities[_activities_DGV.SelectedIndex].Activity.ItemId);
      }

      private void _copyButton_Click(object sender, RoutedEventArgs e)
      {
         AppServices.Clipboard.SetText(ViewModel.Activities[_activities_DGV.SelectedIndex].Message);
      }
   }
}
