using Upsilon.Apps.Passkey.GUI.Presentation.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Adapters
{
   /// <summary>
   /// Runs <c>onBackground</c> on the UI thread whenever the app leaves the foreground
   /// while the element is loaded. Hold-to-reveal relies on <c>Released</c>,
   /// which never fires when the app is backgrounded mid-press.
   /// </summary>
   internal static class BackgroundGuard
   {
      public static void Attach(VisualElement element, Action onBackground)
      {
         ArgumentNullException.ThrowIfNull(element);
         ArgumentNullException.ThrowIfNull(onBackground);

         void handler(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(onBackground);

         element.Loaded += (_, _) =>
         {
            if (AppServices.Lifecycle is { } life)
            {
               life.EnteredBackground += handler;
            }
         };
         element.Unloaded += (_, _) =>
         {
            if (AppServices.Lifecycle is { } life)
            {
               life.EnteredBackground -= handler;
            }

            onBackground();
         };
      }
   }
}
