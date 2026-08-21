using System.Windows;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;

namespace Upsilon.Apps.Passkey.GUI.WPF
{
   /// <summary>
   /// Interaction logic for App.xaml
   /// </summary>
   internal sealed partial class App : Application
   {
      protected override void OnStartup(StartupEventArgs e)
      {
         DispatcherUnhandledException += _onDispatcherUnhandledException;
         AppDomain.CurrentDomain.UnhandledException += _onAppDomainUnhandledException;
         TaskScheduler.UnobservedTaskException += _onUnobservedTaskException;

         Log.Info($"Application starting (PID {Environment.ProcessId}).");

         _ = AppInfo.ConfigFile;
         LocalizationService.Apply(AppInfo.AppSettings.Language);

         if (AppInfo.ConfigLoadHadError)
         {
            AppServices.Dialogs.Warn(Strings.Msg_ConfigFileError, Strings.Title_ConfigFileError);
         }

         base.OnStartup(e);
      }

      protected override void OnExit(ExitEventArgs e)
      {
         ArgumentNullException.ThrowIfNull(e);

         // Close any open vault and clear owned clipboard content before the
         // process tears down, in case MainWindow.Closed did not run first.
         AppServices.Session.EndSession();

         Log.Info($"Application exiting with code {e.ApplicationExitCode}.");
         Log.Flush();

         base.OnExit(e);
      }

      private static void _onDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
      {
         Log.Error(e.Exception, "Unhandled UI exception");
         // The application keeps running; the caller has already shown any
         // feedback it needed. Marking as handled prevents the default crash.
         e.Handled = true;
      }

      private static void _onAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
      {
         if (e.ExceptionObject is Exception exception)
         {
            Log.Error(exception, $"Unhandled AppDomain exception (terminating: {e.IsTerminating})");
         }
         else
         {
            Log.Error($"Unhandled non-CLR AppDomain exception (terminating: {e.IsTerminating}).");
         }

         Log.Flush();
      }

      private static void _onUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
      {
         Log.Error(e.Exception, "Unobserved task exception");
         e.SetObserved();
      }
   }
}
