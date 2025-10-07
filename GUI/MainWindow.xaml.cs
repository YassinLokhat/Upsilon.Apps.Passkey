using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Upsilon.Apps.Passkey.GUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      public static string AppTitle
      {
         get
         {
            var package = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            var packageVersion = package.Version?.ToString(2);

            return $"{package.Name} v{packageVersion}";
         }
      }

      public MainWindow()
      {
         InitializeComponent();

         DataContext = new MainViewModel { WindowTitle = AppTitle };

         this.Loaded += MainWindow_Loaded;
      }

      private void MainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         SetDarkMode();
      }

      #region SetDarkMode
      private void SetDarkMode()
      {
         SetDarkTitleBar();

         var dictionaries = Application.Current.Resources.MergedDictionaries;
         dictionaries.Clear();

         var themeDict = new ResourceDictionary
         {
            Source = new Uri($"Themes/DarkTheme.xaml", UriKind.Relative)
         };

         dictionaries.Add(themeDict);
      }

      private void SetDarkTitleBar()
      {
         var hwnd = new WindowInteropHelper(this).Handle;

         if (hwnd == IntPtr.Zero)
            return;

         int attribute = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE
         int useImmersiveDarkMode = 1;
         DwmSetWindowAttribute(hwnd, attribute, ref useImmersiveDarkMode, sizeof(int));
      }

      [DllImport("dwmapi.dll", PreserveSig = true)]
      private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
      #endregion
   }
}