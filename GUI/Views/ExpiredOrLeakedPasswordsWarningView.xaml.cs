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
   /// Interaction logic for ExpiredOrLeakedPasswordsWarningView.xaml
   /// </summary>
   public partial class ExpiredOrLeakedPasswordsWarningView : Window
   {
      public ExpiredOrLeakedPasswordsWarningView()
      {
         InitializeComponent();

         Loaded += (s, e) => DarkMode.SetDarkMode(this);
      }
   }
}
