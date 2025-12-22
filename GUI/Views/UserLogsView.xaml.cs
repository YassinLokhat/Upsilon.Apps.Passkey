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
   public partial class UserLogsView : Window
   {
      private readonly UserLogsViewModel _viewModel;

      private UserLogsView()
      {
         InitializeComponent();

         DataContext = _viewModel = new();

         _eventType_CB.ItemsSource = Enum.GetValues<LogEventType>()
            .Cast<LogEventType>()
            .Select(x => x.ToReadableString());
         _eventType_CB.SelectedIndex = 0;

         _logList_LB.ItemsSource = _viewModel.Logs;

         Loaded += _userLogsView_Loaded;
      }

      private void _userLogsView_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);
      }

      public static void ShowLogsDialog(Window owner)
      {
         UserLogsView _userLogsView = new()
         {
            Owner = owner,
         };

         _userLogsView.ShowDialog();
      }

      private void _filterClear_Button_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.FromDateFilter = _viewModel.ToDateFilter = DateTime.Now.Date.AddDays(1);
         _viewModel.EventType = LogEventType.None;
         _viewModel.Message = string.Empty;
         _viewModel.NeedsReview = false;
      }
   }
}
