using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Themes;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Services
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
            Database = null;
            Log.Info("Session ended.");
            _ = LocalizationService.Apply(PasskeyAppInfo.AppSettings.Language);
            _ = ThemeService.Apply(PasskeyAppInfo.AppSettings.Theme);
            SessionChanged?.Invoke(this, EventArgs.Empty);
         }
      }

      public void ApplySessionLanguage() => _applySessionLanguage();

      public void ApplySessionTheme() => _applySessionTheme();

      private void _applySessionLanguage()
         => _ = LocalizationService.ApplyEffective(
            PasskeyAppInfo.AppSettings.Language,
            User?.Settings.Language);

      private void _applySessionTheme()
         => _ = ThemeService.ApplyEffective(
            PasskeyAppInfo.AppSettings.Theme,
            User?.Settings.Theme);
   }
}
