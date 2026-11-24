using System.Windows;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.WPF.Adapters
{
   internal sealed class WpfUiDispatcher : IUiDispatcher
   {
      public bool CheckAccess()
      {
         Dispatcher? dispatcher = Application.Current?.Dispatcher;
         return dispatcher is null || dispatcher.CheckAccess();
      }

      public void Post(Action action)
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

      public void Send(Action action)
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

      public T Send<T>(Func<T> func)
      {
         ArgumentNullException.ThrowIfNull(func);
         Dispatcher? dispatcher = Application.Current?.Dispatcher;
         return dispatcher is null || dispatcher.CheckAccess() ? func() : dispatcher.Invoke(func);
      }
   }
}
