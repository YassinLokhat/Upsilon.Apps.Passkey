using System.Reflection;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.GUI.WPF.Adapters
{
   internal sealed class WpfAppInfo : IAppInfo
   {
      private readonly AssemblyName _name = typeof(WpfAppInfo).Assembly.GetName();

      public string Name => _name.Name ?? "Upsilon.Apps.Passkey.GUI.WPF";

      public string Version => _name.Version?.ToString(3) ?? "0.0.0";

      public string Title => AppInfo.Title;
   }

   internal sealed class WpfAppLog : IAppLog
   {
      public void Info(string message) => Log.Info(message);

      public void Warn(string message) => Log.Warn(message);

      public void Error(string message, Exception? exception = null)
      {
         if (exception is null)
         {
            Log.Error(message);
         }
         else
         {
            Log.Error(exception, message);
         }
      }
   }
}
