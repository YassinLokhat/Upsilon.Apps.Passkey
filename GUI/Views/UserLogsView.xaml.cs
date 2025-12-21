using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Upsilon.Apps.Passkey.GUI.Themes;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for UserLogsView.xaml
   /// </summary>
   public partial class UserLogsView : Window
   {
      private UserLogsView()
      {
         InitializeComponent();

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

   }
}
