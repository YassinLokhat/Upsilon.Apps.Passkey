using Upsilon.Apps.Passkey.GUI.WPF.Helper;
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
         SessionChanged?.Invoke(this, EventArgs.Empty);
      }

      public void EndSession(bool closeDatabase = true)
      {
         ClipboardManager.ClearIfStillOwned();

         if (Database is null)
         {
            return;
         }

         if (closeDatabase)
         {
            Database.Close();
         }

         Database = null;
         Log.Info("Session ended.");
         SessionChanged?.Invoke(this, EventArgs.Empty);
      }
   }
}
