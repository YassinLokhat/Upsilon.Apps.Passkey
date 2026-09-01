using System.Diagnostics;
using System.IO;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Helpers
{
   /// <summary>
   /// Application-wide logger. Writes rolling daily files under the platform
   /// logs directory (Windows: %LocalAppData%\Passkey\logs; Android: AppData/logs).
   /// </summary>
   internal static class Log
   {
      private static readonly TraceSource _source = new("Passkey", SourceLevels.All);

      static Log()
      {
         _source.Listeners.Clear();

         try
         {
            string directory = AppPaths.LogsDirectory;
            _ = Directory.CreateDirectory(directory);

            string file = Path.Join(directory, $"app-{DateTime.Now:yyyyMMdd}.log");
            _ = _source.Listeners.Add(new TextWriterTraceListener(file, "PasskeyFile"));
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or ArgumentNullException
            or PathTooLongException
            or DirectoryNotFoundException
            or IOException
            or UnauthorizedAccessException
            or FileNotFoundException
            or NotSupportedException
            or PlatformNotSupportedException)
         {
            _ = _source.Listeners.Add(new DefaultTraceListener());
         }

         Trace.AutoFlush = true;
      }

      public static void Info(string message) => _source.TraceEvent(TraceEventType.Information, 0, _format(message));

      public static void Warn(string message) => _source.TraceEvent(TraceEventType.Warning, 0, _format(message));

      public static void Error(string message) => _source.TraceEvent(TraceEventType.Error, 0, _format(message));

      public static void Error(Exception exception, string message)
         => _source.TraceEvent(TraceEventType.Error, 0, _format($"{message}: {exception}"));

      public static void Flush()
      {
         foreach (TraceListener listener in _source.Listeners)
         {
            listener.Flush();
         }
      }

      private static string _format(string message)
         => $"{DateTime.Now:HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {message}";
   }
}
