using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views.Controls
{
   /// <summary>
   /// Interaction logic for AutoCompleteTextBox.xaml
   /// </summary>
   public partial class AutoCompleteTextBox : TextBox
   {
      public static readonly DependencyProperty ItemsSourceProperty =
    DependencyProperty.RegisterAttached("ItemsSource", typeof(IEnumerable), typeof(AutoCompleteTextBox),
        new PropertyMetadata(null, _onItemsSourceChanged));

      public AutoCompleteTextBox()
      {
         InitializeComponent();
      }

      public static IEnumerable GetItemsSource(DependencyObject obj) => (IEnumerable)obj.GetValue(ItemsSourceProperty);
      public static void SetItemsSource(DependencyObject obj, IEnumerable value) => obj.SetValue(ItemsSourceProperty, value);

      private static void _onItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         if (d is TextBox textBox)
         {
            textBox.TextChanged -= _textBox_TextChanged;

            if (e.NewValue is not null)
            {
               textBox.TextChanged += _textBox_TextChanged;
            }
            _attachPopup(textBox);
         }
      }

      private static void _textBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         TextBox textBox = (TextBox)sender;
         Popup popup = _getPopup(textBox);
         if (popup is null) return;

         popup.IsOpen = false;

         IEnumerable<object>? items = GetItemsSource(textBox)?.Cast<object>().Where(i =>
             i?.ToString()?.Contains(textBox.Text, StringComparison.OrdinalIgnoreCase) == true);

         ListBox listBox = (ListBox)popup.Child;
         listBox.ItemsSource = items;
         popup.IsOpen = items?.Any() == true;
      }

      private static readonly Dictionary<TextBox, Popup> _popups = [];

      private static Popup _getPopup(TextBox textBox)
      {
         _ = _popups.TryGetValue(textBox, out Popup? popup);

         if (popup is null)
         {
            popup = new Popup { PlacementTarget = textBox, Placement = PlacementMode.Bottom };
            ListBox listBox = new() { MaxHeight = 150 };
            listBox.SelectionChanged += (s, args) =>
            {
               if (listBox.SelectedItem is not null)
               {
                  IdentifierViewModel? viewModel = textBox.DataContext as IdentifierViewModel;
                  _ = (viewModel?.Identifier = textBox.Text = listBox.SelectedItem.ToString() ?? string.Empty);
                  popup.IsOpen = false;
                  textBox.Select(textBox.Text?.Length ?? 0, 0);
               }
            };
            listBox.PreviewKeyDown += (s, args) =>
            {
               if (args.Key is Key.Enter or Key.Tab)
               {
                  ListBox listBox2 = (ListBox)s;
                  if (listBox2.SelectedItem is not null)
                  {
                     IdentifierViewModel? viewModel = textBox.DataContext as IdentifierViewModel;
                     _ = (viewModel?.Identifier = textBox.Text = listBox2.SelectedItem.ToString() ?? string.Empty);
                     popup.IsOpen = false;
                  }
                  args.Handled = true;
               }
               else if (args.Key == Key.Escape)
               {
                  popup.IsOpen = false;
                  args.Handled = true;
               }
            };
            popup.Child = listBox;
         }

         popup.IsOpen = false;

         _popups[textBox] = popup;

         return popup;
      }

      private static void _attachPopup(TextBox textBox)
      {
         textBox.KeyDown += (s, e) =>
         {
            Popup popup = _getPopup(textBox);
            if (e.Key == Key.Down) popup.IsOpen = true;
         };
         textBox.LostFocus += (s, e) => _getPopup(textBox)?.IsOpen = false;
      }
   }
}
