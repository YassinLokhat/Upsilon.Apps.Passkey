using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Restricts a TextBox to non-negative integers (preview, change, and paste).
   /// </summary>
   internal static class NumericTextBoxHelper
   {
      private static readonly Regex _regex = new("[^0-9]+");

      private static bool _isTextAllowed(string text)
      {
         bool isValid = !_regex.IsMatch(text);

         if (isValid)
         {
            isValid = int.TryParse(text, out _);
         }

         return isValid;
      }

      public static void TextChanged(object sender, TextChangedEventArgs e)
      {
         TextBox textBox = (TextBox)sender;

         e.Handled = !_isTextAllowed(textBox.Text);

         if (e.Handled)
         {
            textBox.Text = textBox.Text.Replace(" ", "", StringComparison.Ordinal);
         }
      }

      public static void PreviewTextInput(object sender, TextCompositionEventArgs e)
      {
         e.Handled = !_isTextAllowed(e.Text);
      }

      public static void Pasting(object sender, DataObjectPastingEventArgs e)
      {
         if (e.DataObject.GetDataPresent(typeof(string)))
         {
            string text = (string)e.DataObject.GetData(typeof(string));
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
   }
}
