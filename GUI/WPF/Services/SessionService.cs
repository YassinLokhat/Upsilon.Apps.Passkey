using Upsilon.Apps.Passkey.GUI.WPF.Helper;
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
         SessionChanged?.Invoke(this, EventArgs.Empty);
      }

      public void EndSession()
      {
         SensitiveClipboard.ClearIfStillOwned();

         if (Database is null) return;

         try
         {
            Database.Close();
         }
         catch (Exception ex)
         {
            Log.Error(ex, "Failed to close database cleanly");
         }

         Database = null;
         Log.Info("Session ended.");
         SessionChanged?.Invoke(this, EventArgs.Empty);
      }
   }
}
