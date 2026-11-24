using System.Diagnostics;
using System.Globalization;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>
   /// Daily log file under <see cref="IAppPaths.LogDirectory"/> built on
   /// <see cref="TraceSource"/> (no external dependency). Falls back to the
   /// default trace listener when the directory is not writable.
   /// </summary>
   internal sealed class TraceFileAppLog : IAppLog, IDisposable
   {
      private readonly TraceSource _source = new("Passkey", SourceLevels.All);

      public TraceFileAppLog(string logDirectory)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
         _source.Listeners.Clear();

         try
         {
            _ = Directory.CreateDirectory(logDirectory);
            string file = Path.Join(logDirectory, $"app-{DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.log");
            _ = _source.Listeners.Add(new TextWriterTraceListener(file, "PasskeyFile"));
         }
         catch (Exception ex) when (ex is ArgumentException
            or PathTooLongException
            or DirectoryNotFoundException
            or IOException
            or UnauthorizedAccessException
            or NotSupportedException)
         {
            // Logging must never crash the application.
            _ = _source.Listeners.Add(new DefaultTraceListener());
         }

         Trace.AutoFlush = true;
      }

      public void Info(string message) => _source.TraceEvent(TraceEventType.Information, 0, _format(message));

      public void Warn(string message) => _source.TraceEvent(TraceEventType.Warning, 0, _format(message));

      public void Error(string message, Exception? exception = null)
         => _source.TraceEvent(TraceEventType.Error, 0, _format(exception is null ? message : $"{message}: {exception}"));

      public void Dispose()
      {
         _source.Flush();
         _source.Close();
      }

      private static string _format(string message)
         => $"[{DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)}] {message}";
   }

   /// <summary>Discards everything (tests, or before the host wires a log).</summary>
   internal sealed class NullAppLog : IAppLog
   {
      public void Info(string message)
      {
      }

      public void Warn(string message)
      {
      }

      public void Error(string message, Exception? exception = null)
      {
      }
   }
}
