namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages
{
   /// <summary>
   /// Minimum window size while a page is shown. Only applied on Windows: elsewhere
   /// the page always fills the screen. When the top page goes away, the minimum of
   /// the page underneath it applies again.
   /// </summary>
   internal static class WindowMinimumSize
   {
      public static readonly BindableProperty WidthProperty =
         BindableProperty.CreateAttached("Width", typeof(double), typeof(WindowMinimumSize), 0d, propertyChanged: _onSizeChanged);

      public static readonly BindableProperty HeightProperty =
         BindableProperty.CreateAttached("Height", typeof(double), typeof(WindowMinimumSize), 0d, propertyChanged: _onSizeChanged);

      private static readonly List<Page> _shownPages = [];

      public static double GetWidth(BindableObject page) => (double)page.GetValue(WidthProperty);

      public static void SetWidth(BindableObject page, double value) => page.SetValue(WidthProperty, value);

      public static double GetHeight(BindableObject page) => (double)page.GetValue(HeightProperty);

      public static void SetHeight(BindableObject page, double value) => page.SetValue(HeightProperty, value);

      private static void _onSizeChanged(BindableObject bindable, object oldValue, object newValue)
      {
#if WINDOWS
         if (bindable is not Page page)
         {
            return;
         }

         page.Appearing -= _onAppearing;
         page.Disappearing -= _onDisappearing;
         page.Appearing += _onAppearing;
         page.Disappearing += _onDisappearing;
#endif
      }

      private static void _onAppearing(object? sender, EventArgs e)
      {
         if (sender is not Page page)
         {
            return;
         }

         _ = _shownPages.Remove(page);
         _shownPages.Add(page);
         _apply(page.Window);
      }

      private static void _onDisappearing(object? sender, EventArgs e)
      {
         if (sender is Page page && _shownPages.Remove(page))
         {
            _apply(page.Window ?? _shownPages.LastOrDefault()?.Window);
         }
      }

      private static void _apply(Window? window)
      {
         if (window is null)
         {
            return;
         }

         if (_shownPages.LastOrDefault() is { } top)
         {
            window.MinimumWidth = GetWidth(top);
            window.MinimumHeight = GetHeight(top);
         }
         else
         {
            window.ClearValue(Window.MinimumWidthProperty);
            window.ClearValue(Window.MinimumHeightProperty);
         }
      }
   }
}
