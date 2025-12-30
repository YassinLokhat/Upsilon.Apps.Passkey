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
      public string ItemId { get; private set; } = string.Empty;

      private UserActivitiesView()
      {
         InitializeComponent();

         DataContext = _viewModel = new();

         _eventType_CB.ItemsSource = Enum.GetValues<ActivityEventType>()
            .Cast<ActivityEventType>()
            .Select(x => x.ToReadableString());
         _eventType_CB.SelectedIndex = 0;

         _activities_DGV.ItemsSource = _viewModel.Activities;

         Loaded += _userActivitiesView_Loaded;
      }

      private void _userActivitiesView_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);
      }

      public static string? ShowActivitiesDialog(Window owner)
      {
         UserActivitiesView _userActivitiesView = new()
         {
            Owner = owner,
         };

         return (_userActivitiesView.ShowDialog() ?? false) ? _userActivitiesView.ItemId : null;
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
         ItemId = _viewModel.Activities[_activities_DGV.SelectedIndex].Activity.ItemId;

         DialogResult = true;
      }
   }
}
