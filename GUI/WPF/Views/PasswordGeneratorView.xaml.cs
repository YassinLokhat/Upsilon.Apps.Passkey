using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for PasswordGenerator.xaml
   /// </summary>
   public partial class PasswordGenerator : Window
   {
      private readonly PasswordGeneratorViewModel _viewModel;

      public string? GeneratedPassword { get; private set; }

      internal PasswordGenerator()
      {
         InitializeComponent();

         DataContext = _viewModel = new PasswordGeneratorViewModel();
         _viewModel.InsertRequested += _viewModel_InsertRequested;

         Loaded += (s, e) => this.PostLoadSetup();
      }

      public static string? ShowGeneratePasswordDialog(Window owner)
      {
         PasswordGenerator _passwordGenerator = new()
         {
            Owner = owner,
         };

         return _passwordGenerator.ShowDialog() ?? false ? _passwordGenerator.GeneratedPassword : null;
      }

      private void _viewModel_InsertRequested(object? sender, EventArgs e)
      {
         GeneratedPassword = _viewModel.GeneratedPassword;
         DialogResult = true;
      }

      private void _length_TextBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         NumericTextBoxHelper.TextChanged(sender, e);
      }

      private void _length_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
      {
         NumericTextBoxHelper.PreviewTextInput(sender, e);
      }

      private void _length_TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
      {
         NumericTextBoxHelper.Pasting(sender, e);
      }
   }
}
