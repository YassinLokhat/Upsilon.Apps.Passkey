using System.Diagnostics;
using System.IO;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Application-wide logger built on top of <see cref="TraceSource"/>.
   /// No external dependency is used; entries are written to a rolling daily
   /// file located under <c>%LocalAppData%\Passkey\logs</c>.
   /// </summary>
   internal static class Log
   {
      private static readonly TraceSource _source = new("Passkey", SourceLevels.All);

      static Log()
      {
         _source.Listeners.Clear();

         try
         {
            string directory = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               "Passkey",
               "logs");

            _ = Directory.CreateDirectory(directory);

            string file = Path.Combine(directory, $"app-{DateTime.Now:yyyyMMdd}.log");

            TextWriterTraceListener fileListener = new(file, "PasskeyFile");
            _ = _source.Listeners.Add(fileListener);
         }
         catch
         {
            // Logging must never crash the application: silently fall back to
            // an in-memory listener when the file cannot be created.
            _ = _source.Listeners.Add(new DefaultTraceListener());
         }

         Trace.AutoFlush = true;
      }

      public static void Info(string message) => _source.TraceEvent(TraceEventType.Information, 0, _format(message));

      public static void Warn(string message) => _source.TraceEvent(TraceEventType.Warning, 0, _format(message));

      public static void Error(string message) => _source.TraceEvent(TraceEventType.Error, 0, _format(message));

      public static void Error(Exception exception, string message)
         => _source.TraceEvent(TraceEventType.Error, 0, _format($"{message}: {exception}"));

      public static void Flush() => _source.Flush();

      private static string _format(string message)
         => $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
   }
}
