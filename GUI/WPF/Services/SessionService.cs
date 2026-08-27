using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Services
{
   internal sealed class SessionService : ISessionService
   {
      public IDatabase? Database { get; private set; }

      public IUser? User => Database?.User;

      public event EventHandler? SessionChanged;

      public void StartSession(IDatabase database)
      {
         ArgumentNullException.ThrowIfNull(database);

         EndSession();

         Database = database;
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
            if (closeDatabase)
            {
               Database.Close();
            }
         }
         finally
         {
            // Clear before Apply so ApplyEffective callers cannot see a stale user
            // override; always restore app language/theme even if Close throws
            // (already disposed).
            Database = null;
            Log.Info("Session ended.");
            _ = LocalizationService.Apply(AppInfo.AppSettings.Language);
            _ = ThemeService.Apply(AppInfo.AppSettings.Theme);
            SessionChanged?.Invoke(this, EventArgs.Empty);
         }
      }

      /// <summary>
      /// Re-applies app language overridden by the logged-in user's preference (if any).
      /// Call after login completes when <see cref="User"/> becomes available.
      /// </summary>
      public void ApplySessionLanguage()
         => _applySessionLanguage();

      /// <summary>
      /// Re-applies app theme overridden by the logged-in user's preference (if any).
      /// Call after login completes when <see cref="User"/> becomes available.
      /// </summary>
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
