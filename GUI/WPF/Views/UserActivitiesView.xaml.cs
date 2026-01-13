using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for UserLogsView.xaml
   /// </summary>
   public partial class UserActivitiesView : Window
   {
      internal readonly UserActivitiesViewModel ViewModel;

      internal UserActivitiesView(bool needsReviewFilter)
      {
         InitializeComponent();

         DataContext = ViewModel = new()
         {
            NeedsReview = needsReviewFilter,
         };

         _eventType_CB.ItemsSource = Enum.GetValues<ActivityEventType>()
            .Cast<ActivityEventType>()
            .Select(x => x.ToReadableString());
         _eventType_CB.SelectedIndex = 0;

         _activities_DGV.ItemsSource = ViewModel.Activities;

         Loaded += (s, e) => DarkMode.SetDarkMode(this);
      }

      private void _filterClear_Button_Click(object sender, RoutedEventArgs e)
      {
         ViewModel.ClearFilters();
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         MainViewModel.GoToItem?.Invoke(ViewModel.Activities[_activities_DGV.SelectedIndex].Activity.ItemId);
      }
   }
}
