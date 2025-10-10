using System.Text.RegularExpressions;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.PassKey.Core.Public.Utils;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for PasswordGenerator.xaml
   /// </summary>
   public partial class PasswordGenerator : Window
   {
      private readonly PasswordGeneratorViewModel _viewModel;

      private string? _generatedPassword = null;
      public string? GeneratedPassword
      {
         get => _generatedPassword;
         private set
         {
            if (_generatedPassword == null)
            {
               _viewModel.AllowInsert = Visibility.Visible;
            }

            _generatedPassword = value;
         }
      }

      public PasswordGenerator()
      {
         InitializeComponent();

         DataContext = _viewModel = new PasswordGeneratorViewModel(new PasswordFactory());

         this.Loaded += _passwordGenerator_Loaded;
      }

      public static string? ShowGeneratePasswordDialog(Window owner)
      {
         PasswordGenerator _passwordGenerator = new()
         {
            Owner = owner,
            GeneratedPassword = string.Empty
         };

         if (_passwordGenerator.ShowDialog() ?? false)
         {
            return _passwordGenerator.GeneratedPassword;
         }

         return null;
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

      private void _insertMenuItem_Click(object sender, RoutedEventArgs e)
      {
         _copyMenuItem_Click(sender, e);

         GeneratedPassword = _viewModel.GeneratedPassword;
         this.DialogResult = true;
      }

      private void _regenerateMenuItem_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.GeneratePassword();
      }

      private void _copyMenuItem_Click(object sender, RoutedEventArgs e)
      {
         Clipboard.SetText(_viewModel.GeneratedPassword);
      }
   }
}
