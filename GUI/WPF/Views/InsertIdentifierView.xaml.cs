using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using InsertIdentifierViewModel = Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools.InsertIdentifierViewModel;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for InsertIdentifierView.xaml
   /// </summary>
   internal sealed partial class InsertIdentifierView : Window
   {
      private readonly InsertIdentifierViewModel _viewModel;
      private IIdentifier? _selectedIdentifier;

      private InsertIdentifierView(IEnumerable<IIdentifier> identifiers, IIdentifier? identifier)
      {
         InitializeComponent();

         DataContext = _viewModel = new(identifiers, identifier);
         IdentifierType identifierType = identifier?.Type ?? (IdentifierType)0xFF;

         foreach (object? child in _identifierTypes_SP.Children)
         {
            if (child is RadioButton button)
            {
               IdentifierType type = Enum.GetValues<IdentifierType>().FirstOrDefault(x => Enum.GetName(x) == $"{button.Tag}");
               string glyph = $"{button.Tag}" != "All" ? IdentifierViewModel.TypeGlyphs[type] : IdentifierViewModel.AllTypeGlyph;
               button.Content = $"{glyph} {button.Content}";
               button.IsChecked = $"{button.Tag}" == "All" || identifierType == type;
            }
         }

         _identifiers_LB.ItemsSource = _viewModel.Identifiers;
         _ = _identifier_TB.Focus();

         Loaded += (s, e) => this.PostLoadSetup();
      }

public static void ShowInsertIdentifierView(IdentifierViewModel viewModel)
      {
         IIdentifier? identifier = InsertIdentifierDialog(
            AccountViewModel.IdentifierAutoCompleteList ?? [],
            viewModel.ToIdentifier());

         if (identifier is null
            || string.IsNullOrEmpty(identifier.Value))
         {
            return;
         }

         viewModel.Type = identifier.Type;
         viewModel.Identifier = identifier.Value;
      }

      internal static IIdentifier? InsertIdentifierDialog(IEnumerable<IIdentifier> identifiers, IIdentifier? identifier = null)
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

         _viewModel.Type = idType is not null
            && Enum.TryParse(idType, ignoreCase: false, out IdentifierType type)
            ? type
            : IdentifierViewModel.AllIdentifierType;
      }

      private void _clearFilter_Button_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.Identifier = string.Empty;
      }
   }
}
