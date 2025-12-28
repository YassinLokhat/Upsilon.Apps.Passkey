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
      public string ItemId { get; private set; } = string.Empty;

      private UserLogsView()
      {
         InitializeComponent();

         DataContext = _viewModel = new();

         _eventType_CB.ItemsSource = Enum.GetValues<LogEventType>()
            .Cast<LogEventType>()
            .Select(x => x.ToReadableString());
         _eventType_CB.SelectedIndex = 0;

         _logs_DGV.ItemsSource = _viewModel.Logs;

         Loaded += _userLogsView_Loaded;
      }

      private void _userLogsView_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);
      }

      public static string? ShowLogsDialog(Window owner)
      {
         UserLogsView _userLogsView = new()
         {
            Owner = owner,
         };

         return (_userLogsView.ShowDialog() ?? false) ? _userLogsView.ItemId : null;
      }

      private void _filterClear_Button_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.FromDateFilter = _viewModel.ToDateFilter = DateTime.Now.Date.AddDays(1);
         _viewModel.EventType = LogEventType.None;
         _viewModel.Message = string.Empty;
         _viewModel.NeedsReview = false;
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         ItemId = _viewModel.Logs[_logs_DGV.SelectedIndex].Log.ItemId;

         DialogResult = true;
      }
   }
}
