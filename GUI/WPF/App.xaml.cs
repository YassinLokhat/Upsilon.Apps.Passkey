using System.Windows;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Adapters;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using WpfAppServices = Upsilon.Apps.Passkey.GUI.WPF.Services.AppServices;

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

         // Touch so CA1812 sees BAML-created types as used (factories are never invoked).
         _ = XamlBamlCreatedTypeAnchors.Factories.Length;

         AppServices.Ui = new WpfUiDispatcher();
         AppServices.Timers = new WpfUiTimerFactory();
         AppServices.Paths = new WpfAppPaths();
         AppServices.Launcher = new WpfLauncher();
         AppServices.VaultSources = new PathVaultSourceFactory();
         AppServices.Cryptography = WpfAppServices.Cryptography;
         AppServices.Serialization = WpfAppServices.Serialization;
         AppServices.PasswordFactory = WpfAppServices.PasswordFactory;
         AppServices.SecretProtector = WpfAppServices.SecretProtector;

         _ = AppInfo.ConfigFile;
         _ = LocalizationService.Apply(AppInfo.AppSettings.Language);
         _ = ThemeService.Apply(AppInfo.AppSettings.Theme);

         // Refresh an existing .pkbf only — never a first full build (too heavy).
         WpfAppServices.OfflineLeakFilterUpdate.TryStartAutoUpdate();

         base.OnStartup(e);
      }

      protected override void OnExit(ExitEventArgs e)
      {
         ArgumentNullException.ThrowIfNull(e);

         WPF.Services.OfflineLeakFilterUpdateService update = WpfAppServices.OfflineLeakFilterUpdate;
         if (update.ContinueThroughExit)
         {
            // Consented background continue — wait without cancelling.
            if (!update.WaitUntilIdle(TimeSpan.FromSeconds(30)))
            {
               Log.Warn("Offline leak filter: still busy after background-continue exit wait.");
            }
         }
         else if (!update.WaitForIdle(TimeSpan.FromSeconds(15)))
         {
            Log.Warn("Offline leak filter: still busy after exit cancel timeout.");
         }

         // Close any open vault and clear owned clipboard content before the
         // process tears down, in case MainWindow.Closed did not run first.
         WpfAppServices.Session.EndSession();
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
