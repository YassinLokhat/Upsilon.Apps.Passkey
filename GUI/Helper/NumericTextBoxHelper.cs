using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Upsilon.Apps.Passkey.GUI.Helper
{
   public static class NumericTextBoxHelper
   {
      private static readonly Regex _regex = new("[^0-9]+"); //regex that matches disallowed text

      private static bool _isTextAllowed(string text)
      {
         bool isValid = !_regex.IsMatch(text);

         if (isValid)
         {
            isValid = int.TryParse(text, out _);
         }

         return isValid;
      }

      public static void Value_TextBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         TextBox textBox = (TextBox)sender;

         e.Handled = !_isTextAllowed(textBox.Text);

         if (e.Handled)
         {
            textBox.Text = textBox.Text.Replace(" ", "");
         }
      }

      public static void Value_TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
      {
         e.Handled = !_isTextAllowed(e.Text);
      }

      public static void Value_TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
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
