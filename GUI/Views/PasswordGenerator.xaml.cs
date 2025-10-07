using System.Text.RegularExpressions;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for PasswordGenerator.xaml
   /// </summary>
   public partial class PasswordGenerator : Window
   {
      public PasswordGenerator()
      {
         InitializeComponent();

         DataContext = new TitleViewModel();

         this.Loaded += _passwordGenerator_Loaded;
      }

      private void _passwordGenerator_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);
      }

      private static readonly Regex _regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
      private static bool _isTextAllowed(string text)
      {
         return !_regex.IsMatch(text);
      }

      private void _length_TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
      {
         e.Handled = !_isTextAllowed(e.Text);
      }

      private void _length_TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
      {
         if (e.DataObject.GetDataPresent(typeof(String)))
         {
            String text = (String)e.DataObject.GetData(typeof(String));
            if (!_isTextAllowed(text))
            {
               e.CancelCommand();
            }
         }
         else
         {
            e.CancelCommand();
         }
      }
   }
}
