using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for InsertIdentifierView.xaml
   /// </summary>
   public partial class InsertIdentifierView : Window
   {
      private readonly InsertIdentifierViewModel _viewModel;
      private string? _selectedIdentifier;

      private InsertIdentifierView(IEnumerable<string> identifiers)
      {
         InitializeComponent();

         DataContext = _viewModel = new(identifiers);
         _identifiers_LB.ItemsSource = _viewModel.Identifiers;
         _ = _identifier_TB.Focus();

         Loaded += (s, e) => this.PostLoadSetup();
      }

      internal static string? InsertIdentifierDialog(IEnumerable<string> identifiers)
      {
         InsertIdentifierView insertIdentifierView = new(identifiers);

         _ = insertIdentifierView.ShowDialog();

         return insertIdentifierView._selectedIdentifier;
      }

      private void _identifier_TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Escape)
         {
            if (string.IsNullOrEmpty(_viewModel.Identifier))
            {
               _selectedIdentifier = null;
               DialogResult = true;
            }
            _viewModel.Identifier = string.Empty;
         }
         else if (e.Key == System.Windows.Input.Key.Enter)
         {
            if (_viewModel.Identifiers.Any()
               && _identifiers_LB.SelectedIndex == -1)
            {
               _identifiers_LB.SelectedIndex = 0;
            }
            else if (!_viewModel.Identifiers.Any())
            {
               _selectedIdentifier = _viewModel.Identifier;
               DialogResult = true;
            }
         }
      }

      private void _identifiers_LB_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         _selectedIdentifier = _identifiers_LB.SelectedItem as string;
         DialogResult = true;
      }
   }
}