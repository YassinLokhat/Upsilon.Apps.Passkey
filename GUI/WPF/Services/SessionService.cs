using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.Warnings;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Services
{
   internal sealed class SessionService : ISessionService
   {
      public IDatabase? Database { get; private set; }

      public IUser? User => Database?.User;

      public WarningBroker Warnings { get; } = new();

      public event EventHandler? SessionChanged;

      public void StartSession(IDatabase database)
      {
         ArgumentNullException.ThrowIfNull(database);

         EndSession();

         Database = database;
         Warnings.Attach(database);

         Log.Info("Session started.");
         _applySessionLanguage();
         _applySessionTheme();
         SessionChanged?.Invoke(this, EventArgs.Empty);
      }

      public void EndSession(bool closeDatabase = true)
      {
         ClipboardManager.ClearIfStillOwned();

         if (Database is null)
         {
            return;
         }

         try
         {
            Warnings.Detach();

            if (closeDatabase)
            {
               Database.Close();
            }
         }
         finally
         {
            Database = null;
            Log.Info("Session ended.");
            _ = LocalizationService.Apply(AppInfo.AppSettings.Language);
            _ = ThemeService.Apply(AppInfo.AppSettings.Theme);
            SessionChanged?.Invoke(this, EventArgs.Empty);
         }
      }

      public void ApplySessionLanguage()
         => _applySessionLanguage();

      public void ApplySessionTheme()
         => _applySessionTheme();

      private void _applySessionLanguage()
      {
         _ = LocalizationService.ApplyEffective(
            AppInfo.AppSettings.Language,
            User?.Settings.Language);
      }

      private void _applySessionTheme()
      {
         _ = ThemeService.ApplyEffective(
            AppInfo.AppSettings.Theme,
            User?.Settings.Theme);
      }
   }
}
