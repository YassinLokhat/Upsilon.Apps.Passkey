using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for InsertIdentifierView.xaml
   /// </summary>
   internal sealed partial class InsertIdentifierView : Window
   {
      private readonly InsertIdentifierViewModel _viewModel;
      private IIdentifier? _selectedIdentifier;

      private InsertIdentifierView(IEnumerable<string> identifiers, string identifier)
      {
         InitializeComponent();

         DataContext = _viewModel = new(identifiers, identifier);

         foreach (var c in _identifierTypes_GB.Children)
         {
            if (c is not Button b)
            {
               IdentifierType type = Enum.GetValues<IdentifierType>().FirstOrDefault(x => Enum.GetName(x) == $"{b.Tag}");
               string glyph = IdentifierViewModel.TypeGlyphs[type];
               b.Content = $"{glyph} {b.Content}";
            }
         }

         _identifiers_LB.ItemsSource = _viewModel.Identifiers;
         _identifier_TB.SelectAll();
         _ = _identifier_TB.Focus();

         Loaded += (s, e) => this.PostLoadSetup();
      }

      internal static IIdentifier? InsertIdentifierDialog(IEnumerable<string> identifiers, string identifier)
      {
         InsertIdentifierView insertIdentifierView = new(identifiers, identifier);

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
               _selectedIdentifier = _viewModel.ToIdentifier();
               DialogResult = true;
            }
         }
      }

      private void _identifiers_LB_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (_identifiers_LB.SelectedItem is not string value)
         {
            return;
         }

         _viewModel.Identifier = value;
         _selectedIdentifier = _viewModel.ToIdentifier();
         DialogResult = true;
      }

      private void _insertIdentifierType_Button_Click(object sender, RoutedEventArgs e)
      {
         string? idType = ((Button)sender).Tag as string;

         if (idType is not null
            && Enum.TryParse(idType, ignoreCase: false, out IdentifierType type))
         {
            _viewModel.Type = type;
         }
      }

      private void _clearFilter_Button_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.Identifier = string.Empty;
      }
   }
}
