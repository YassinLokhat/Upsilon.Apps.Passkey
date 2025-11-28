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
using Upsilon.Apps.Passkey.GUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for QrCodeView.xaml
   /// </summary>
   public partial class QrCodeView : Window
   {
      private QrCodeView(string qrCode, int delay)
      {
         InitializeComponent();

         Title = MainViewModel.AppTitle;

         Loaded += _mainWindow_Loaded;
      }

      public static void ShowQrCode(Window owner, string qrCode, int delay)
      {
         _ = new QrCodeView(qrCode, delay)
         {
            Owner = owner
         }
         .ShowDialog();
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);
      }
   }
}