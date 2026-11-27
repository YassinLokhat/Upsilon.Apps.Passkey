using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Helper
{
    public static class NumericTextBoxHelper
    {
        private static readonly Regex _regex = new("[^0-9]+");

        private static bool _isTextAllowed(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            return !_regex.IsMatch(text) && int.TryParse(text, out _);
        }

        public static void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entry) return;

            
            if (!_isTextAllowed(e.NewTextValue))
            {
                
                entry.Text = e.OldTextValue ?? "";
            }
        }
    }
}