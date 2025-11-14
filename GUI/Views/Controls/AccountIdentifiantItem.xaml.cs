using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for AccountIdentifiantItem.xaml
   /// </summary>
   public partial class AccountIdentifiantItem : UserControl
   {
      public readonly AccountIdentifiantItemViewModel ViewModel;

      public event EventHandler? UpClicked;
      public event EventHandler? DownClicked;
      public event EventHandler? DeleteClicked;

      public AccountIdentifiantItem(AccountIdentifiantItemViewModel viewModel)
      {
         InitializeComponent();

         DataContext = ViewModel = viewModel;
      }

      private void _upButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         UpClicked?.Invoke(this, EventArgs.Empty);
      }

      private void _downButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         DownClicked?.Invoke(this, EventArgs.Empty);
      }

      private void _deleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         DeleteClicked?.Invoke(this, EventArgs.Empty);
      }

      private void _qrCodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         throw new NotImplementedException();
      }
   }
}
