using System.Windows;
using System.Windows.Threading;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Marshals work onto the WPF UI thread (alert scans finish on workers).
   /// </summary>
   internal static class UiThread
   {
      public static void Post(Action action)
      {
         ArgumentNullException.ThrowIfNull(action);

         Dispatcher? dispatcher = Application.Current?.Dispatcher;
         if (dispatcher is null || dispatcher.CheckAccess())
         {
            action();
            return;
         }

         _ = dispatcher.BeginInvoke(action);
      }
   }
}
