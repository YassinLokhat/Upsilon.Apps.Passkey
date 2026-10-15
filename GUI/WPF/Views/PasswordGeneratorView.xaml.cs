using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using PasswordGeneratorViewModel = Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools.PasswordGeneratorViewModel;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for PasswordGenerator.xaml
   /// </summary>
   internal sealed partial class PasswordGenerator : Window, ILanguageAware
   {
      private readonly PasswordGeneratorViewModel _viewModel;

      public string? GeneratedPassword { get; private set; }

      internal PasswordGenerator()
      {
         InitializeComponent();

         DataContext = _viewModel = new PasswordGeneratorViewModel();
         _viewModel.InsertRequested += _viewModel_InsertRequested;

         Loaded += (s, e) => this.PostLoadSetup();
         Closed += (s, e) => _viewModel.Close();
      }

      public void OnLanguageChanged()
         => _viewModel.OnLanguageChanged();

      public static string? ShowGeneratePasswordDialog()
      {
         PasswordGenerator passwordGenerator = new();
         return AppServices.Dialogs.ShowDialog(passwordGenerator) ?? false
            ? passwordGenerator.GeneratedPassword
            : null;
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
