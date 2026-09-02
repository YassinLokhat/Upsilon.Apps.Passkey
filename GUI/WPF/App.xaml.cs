using System.Windows;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;

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
         _ = LocalizationService.Apply(AppInfo.AppSettings.Language);
         _ = ThemeService.Apply(AppInfo.AppSettings.Theme);

         // Refresh an existing .pkbf only — never a first full build (too heavy).
         AppServices.OfflineLeakFilterUpdate.TryStartAutoUpdate();

         base.OnStartup(e);
      }

      protected override void OnExit(ExitEventArgs e)
      {
         ArgumentNullException.ThrowIfNull(e);

         AppServices.OfflineLeakFilterUpdate.Cancel();

         // Close any open vault and clear owned clipboard content before the
         // process tears down, in case MainWindow.Closed did not run first.
         AppServices.Session.EndSession();
         ThemeService.Shutdown();

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
