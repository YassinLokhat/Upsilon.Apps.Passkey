using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
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

      private InsertIdentifierView(IEnumerable<IIdentifier> identifiers, IIdentifier identifier)
      {
         InitializeComponent();

         DataContext = _viewModel = new(identifiers, identifier);

         foreach (var child in _identifierTypes_SP.Children)
         {
            if (child is RadioButton button)
            {
               IdentifierType type = Enum.GetValues<IdentifierType>().FirstOrDefault(x => Enum.GetName(x) == $"{button.Tag}");
               string glyph = IdentifierViewModel.TypeGlyphs[type];
               button.Content = $"{glyph} {button.Content}";
               button.IsChecked = identifier.Type == type;
            }
         }

         _identifiers_LB.ItemsSource = _viewModel.Identifiers;
         _identifier_TB.SelectionStart = 0;
         _identifier_TB.SelectionLength = _identifier_TB.Text.Length;
         _ = _identifier_TB.Focus();

         Loaded += (s, e) => this.PostLoadSetup();
      }

      internal static IIdentifier? InsertIdentifierDialog(IEnumerable<IIdentifier> identifiers, IIdentifier identifier)
      {
         InsertIdentifierView insertIdentifierView = new(identifiers, identifier);
         _ = AppServices.Dialogs.ShowDialog(insertIdentifierView);
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

      private void _insertIdentifierType_RadioButton_Checked(object sender, RoutedEventArgs e)
      {
         string? idType = ((RadioButton)sender).Tag as string;

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
