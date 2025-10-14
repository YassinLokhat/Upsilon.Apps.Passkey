using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for NumericUpDown.xaml
   /// </summary>
   public partial class NumericUpDown : UserControl
   {
      public uint Value
      {
         get => _viewModel.Value;
         set
         {
            _viewModel.Value = value;
            ValueChanged?.Invoke(this, EventArgs.Empty);
         }
      }

      private uint _minValue = 0;
      public uint MinValue
      {
         get => _minValue;
         set
         {
            _minValue = value;

            if (Value < _minValue)
            {
               Value = _minValue;
            }
         }
      }

      private uint _maxValue = int.MaxValue;
      public uint MaxValue
      {
         get => _maxValue;
         set
         {
            _maxValue = value;

            if (Value > _maxValue)
            {
               Value = _maxValue;
            }
         }
      }

      public uint Step { get; set; } = 1;

      private readonly NumericUpDownViewModel _viewModel;

      public event EventHandler? ValueChanged;

      public NumericUpDown()
      {
         InitializeComponent();

         DataContext = _viewModel = new NumericUpDownViewModel();
      }

      private static readonly Regex _regex = new("[^0-9]+"); //regex that matches disallowed text

      private bool _isTextAllowed(string text)
      {
         bool isValid = !_regex.IsMatch(text);
         int value = 0;

         if (isValid)
         {
            isValid = int.TryParse(text, out value);
         }

         if (isValid)
         {
            isValid = MinValue <= value && value <= MaxValue;
         }

         return isValid;
      }

      private void _value_TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
      {
         TextBox s = (TextBox)sender;
         e.Handled = !_isTextAllowed(s.Text + e.Text);
      }

      private void _value_TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
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

      private void _upButton_Click(object sender, RoutedEventArgs e)
      {
         if (Value + Step <= MaxValue)
         {
            Value += Step;
         }
         else
         {
            Value = MaxValue;
         }
      }

      private void _downButton_Click(object sender, RoutedEventArgs e)
      {
         if (Value - Step >= MinValue)
         {
            Value -= Step;
         }
         else
         {
            Value = MinValue;
         }
      }
   }
}
