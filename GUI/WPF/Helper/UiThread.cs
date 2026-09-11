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

      /// <summary>Runs <paramref name="action"/> on the UI thread, blocking until complete when marshalling.</summary>
      public static void Send(Action action)
      {
         ArgumentNullException.ThrowIfNull(action);

         Dispatcher? dispatcher = Application.Current?.Dispatcher;
         if (dispatcher is null || dispatcher.CheckAccess())
         {
            action();
            return;
         }

         dispatcher.Invoke(action);
      }

      public static T Send<T>(Func<T> func)
      {
         ArgumentNullException.ThrowIfNull(func);

         Dispatcher? dispatcher = Application.Current?.Dispatcher;
         if (dispatcher is null || dispatcher.CheckAccess())
         {
            return func();
         }

         return dispatcher.Invoke(func);
      }
   }
}
