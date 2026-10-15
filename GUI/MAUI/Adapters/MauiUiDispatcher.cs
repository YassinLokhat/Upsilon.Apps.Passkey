using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Adapters
{
   internal sealed class MauiUiDispatcher : IUiDispatcher
   {
      public bool CheckAccess() => MainThread.IsMainThread;

      public void Post(Action action)
      {
         ArgumentNullException.ThrowIfNull(action);
         if (MainThread.IsMainThread)
         {
            action();
            return;
         }

         MainThread.BeginInvokeOnMainThread(action);
      }

      public void Send(Action action)
      {
         ArgumentNullException.ThrowIfNull(action);
         if (MainThread.IsMainThread)
         {
            action();
            return;
         }

         MainThread.InvokeOnMainThreadAsync(action).GetAwaiter().GetResult();
      }

      public T Send<T>(Func<T> func)
      {
         ArgumentNullException.ThrowIfNull(func);
         if (MainThread.IsMainThread)
         {
            return func();
         }

         return MainThread.InvokeOnMainThreadAsync(func).GetAwaiter().GetResult();
      }
   }
}
