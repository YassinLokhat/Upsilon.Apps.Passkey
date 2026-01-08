using System.Windows;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for UserLogsView.xaml
   /// </summary>
   public partial class UserActivitiesView : Window
   {
      private readonly UserActivitiesViewModel _viewModel;
      private readonly Action<string> _goToItem;

      internal UserActivitiesView(bool needsReviewFilter, Action<string> goToItem)
      {
         InitializeComponent();

         DataContext = _viewModel = new()
         {
            NeedsReview = needsReviewFilter,
         };
         
         _goToItem = goToItem;

         _eventType_CB.ItemsSource = Enum.GetValues<ActivityEventType>()
            .Cast<ActivityEventType>()
            .Select(x => x.ToReadableString());
         _eventType_CB.SelectedIndex = 0;

         _activities_DGV.ItemsSource = _viewModel.Activities;

         Loaded += (s, e) => DarkMode.SetDarkMode(this);
      }

      private void _filterClear_Button_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.FromDateFilter = _viewModel.ToDateFilter = DateTime.Now.Date.AddDays(1);
         _viewModel.EventType = ActivityEventType.None;
         _viewModel.Message = string.Empty;
         _viewModel.NeedsReview = false;
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         _goToItem(_viewModel.Activities[_activities_DGV.SelectedIndex].Activity.ItemId);
      }
   }
}
