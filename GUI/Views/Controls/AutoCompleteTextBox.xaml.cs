using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
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

            if (e.NewValue != null)
            {
               textBox.TextChanged += _textBox_TextChanged;
            }
            _attachPopup(textBox);
         }
      }

      private static void _textBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         var textBox = (TextBox)sender;
         var popup = _getPopup(textBox);
         if (popup == null) return;

         popup.IsOpen = false;

         var items = GetItemsSource(textBox)?.Cast<object>().Where(i =>
             i?.ToString()?.Contains(textBox.Text, StringComparison.OrdinalIgnoreCase) == true);

         var listBox = (ListBox)popup.Child;
         listBox.ItemsSource = items;
         popup.IsOpen = items?.Any() == true;
      }

      private static readonly Dictionary<TextBox, Popup> _popups = [];

      private static Popup _getPopup(TextBox textBox)
      {
         _popups.TryGetValue(textBox, out Popup? popup);

         if (popup is null)
         {
            popup = new Popup { PlacementTarget = textBox, Placement = PlacementMode.Bottom };
            var listBox = new ListBox { MaxHeight = 150 };
            listBox.SelectionChanged += (s, args) =>
            {
               if (listBox.SelectedItem != null)
               {
                  var viewModel = textBox.DataContext as IdentifierViewModel;
                  viewModel?.Identifier = textBox.Text = listBox.SelectedItem.ToString() ?? string.Empty;
                  popup.IsOpen = false;
                  textBox.Select(textBox.Text?.Length ?? 0, 0);
               }
            };
            listBox.PreviewKeyDown += (s, args) =>
            {
               if (args.Key == Key.Enter || args.Key == Key.Tab)
               {
                  var listBox2 = (ListBox)s;
                  if (listBox2.SelectedItem != null)
                  {
                     var viewModel = textBox.DataContext as IdentifierViewModel;
                     viewModel?.Identifier = textBox.Text = listBox2.SelectedItem.ToString() ?? string.Empty;
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
            var popup = _getPopup(textBox);
            if (e.Key == Key.Down) popup.IsOpen = true;
         };
         textBox.LostFocus += (s, e) => _getPopup(textBox)?.IsOpen = false;
      }
   }
}
