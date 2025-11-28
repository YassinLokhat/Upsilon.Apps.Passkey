using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for PasswordGenerator.xaml
   /// </summary>
   public partial class PasswordGenerator : Window
   {
      private readonly PasswordGeneratorViewModel _viewModel;

      public string? GeneratedPassword { get; private set; } = null;

      private PasswordGenerator()
      {
         InitializeComponent();

         DataContext = _viewModel = new PasswordGeneratorViewModel();
         _insert.Visibility = (MainViewModel.Database is not null
               && MainViewModel.Database.User is not null) ? Visibility.Visible : Visibility.Hidden;

         Loaded += _passwordGenerator_Loaded;
      }

      public static string? ShowGeneratePasswordDialog(Window owner)
      {
         PasswordGenerator _passwordGenerator = new()
         {
            Owner = owner,
         };

         return _passwordGenerator.ShowDialog() ?? false ? _passwordGenerator.GeneratedPassword : null;
      }

      private void _passwordGenerator_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);
      }

      private void _length_TextBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         NumericTextBoxHelper.TextChanged(sender, e);
      }

      private void _length_TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
      {
         NumericTextBoxHelper.PreviewTextInput(sender, e);
      }

      private void _length_TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
      {
         NumericTextBoxHelper.Pasting(sender, e);
      }

      private void _insertMenuItem_Click(object sender, RoutedEventArgs e)
      {
         _copyMenuItem_Click(sender, e);

         GeneratedPassword = _viewModel.GeneratedPassword;
         DialogResult = true;
      }

      private void _regenerateMenuItem_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.GeneratePassword();
      }

      private void _copyMenuItem_Click(object sender, RoutedEventArgs e)
      {
         QrCodeHelper.CopyToClipboard(_viewModel.GeneratedPassword);
      }
   }
}
