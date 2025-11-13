using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for ServiceView.xaml
   /// </summary>
   public partial class ServiceView : UserControl
   {
      public ServiceView()
      {
         InitializeComponent();
      }

      internal void SetDataContext(ServiceViewModel? serviceViewModel)
      {
         if (serviceViewModel is null) return;

         DataContext = serviceViewModel;
         _accounts_LB.ItemsSource = serviceViewModel.Accounts;
      }

      private void _accounts_LB_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         _ = MainViewModel.User.ItemId;
         _account_AV.DataContext = (AccountViewModel)_accounts_LB.SelectedItem;
      }

      private void _addAccount_Button_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         throw new NotImplementedException();
      }
   }
}
