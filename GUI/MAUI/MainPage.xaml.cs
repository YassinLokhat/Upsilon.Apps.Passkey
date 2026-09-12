namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   internal sealed partial class MainPage : ContentPage
   {
      int _count;

      public MainPage()
      {
         InitializeComponent();
      }

      private void _onCounterClicked(object? sender, EventArgs e)
      {
         _count++;

         if (_count == 1)
            CounterBtn.Text = $"Clicked {_count} time";
         else
            CounterBtn.Text = $"Clicked {_count} times";

         SemanticScreenReader.Announce(CounterBtn.Text);
      }
   }
}
