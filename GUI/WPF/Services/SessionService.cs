using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.OSSpecific;
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
            // override; always restore app language even if Close throws (already disposed).
            Database = null;
            Log.Info("Session ended.");
            _ = LocalizationService.Apply(AppInfo.AppSettings.Language);
            SessionChanged?.Invoke(this, EventArgs.Empty);
         }
      }

      /// <summary>
      /// Re-applies app language overridden by the logged-in user's preference (if any).
      /// Call after login completes when <see cref="User"/> becomes available.
      /// </summary>
      public void ApplySessionLanguage()
         => _applySessionLanguage();

      private void _applySessionLanguage()
      {
         _ = LocalizationService.ApplyEffective(
            AppInfo.AppSettings.Language,
            User?.Settings.Language);
      }
   }
}
