using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Utils;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   internal sealed partial class App : Application
   {
      private readonly MauiAppLifecycle _lifecycle = new();

      public App()
      {
         InitializeComponent();
         AppServices.Lifecycle = _lifecycle;
         _ = AppServices.Settings?.Load();
         _ = LocalizationService.Apply(AppServices.Settings?.Current.Language ?? LocalizationService.SystemCode);
         AppServices.Theme?.Apply(AppServices.Settings?.Current.Theme ?? ThemePreferences.SystemCode);

         if (AppServices.Capabilities.SupportsOfflineLeakFilter && AppServices.Settings is { } settings)
         {
            AppServices.PasswordFactory = new PasswordFactory(settings.Current.LeakFilterConfig);
            // Refresh an existing .pkbf only; a first full build is always an explicit choice.
            AppServices.OfflineLeakFilterUpdate.TryStartAutoUpdate();
         }
      }

      protected override Window CreateWindow(IActivationState? activationState)
      {
         Window window = new(new AppShell());
         window.Destroying += _onWindowDestroying;
         return window;
      }

      private static void _onWindowDestroying(object? sender, EventArgs e)
      {
         // A .pkbf build holds the filter file open for writing: stop it before the process goes away.
         if (!AppServices.OfflineLeakFilterUpdate.WaitForIdle(TimeSpan.FromSeconds(15)))
         {
            AppServices.Log.Warn("Offline leak filter: still busy after exit cancel timeout.");
         }

         // Closing the database queues the final write-back; wait for it so the
         // last save reaches the durable source before the process exits.
         AppServices.Session.EndSession();
         if (!AppServices.Session.FlushWriteBacksAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult())
         {
            AppServices.Log.Warn("Exiting with a vault write-back still pending.");
         }
      }

      protected override void OnSleep()
      {
         base.OnSleep();
#if ANDROID
         // On Windows OnSleep also fires on minimize; the WPF client keeps the
         // half-open login and the clipboard countdown in that case.
         AppServices.Clipboard?.ClearIfStillOwned();
         _lifecycle.NotifyEnteredBackground();
         // Android may kill a backgrounded process: get pending edits to the
         // durable source while the session stays open.
         _ = AppServices.Session.FlushToSourceAsync();
#endif
      }

      protected override void OnResume()
      {
         base.OnResume();
         _lifecycle.NotifyResumed();
      }
   }
}
