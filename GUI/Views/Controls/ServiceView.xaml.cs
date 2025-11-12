using System.Windows.Controls;
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

      internal void SetDataContext(ServiceViewModel serviceViewModel)
      {
         DataContext = serviceViewModel;
         _accounts_LB.ItemsSource = serviceViewModel.Accounts;
      }
   }
}
