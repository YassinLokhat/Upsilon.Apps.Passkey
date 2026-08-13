using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.UnitTests.Gui.Fakes
{
   internal sealed class FakeSessionService : ISessionService
   {
      public IDatabase? Database { get; set; }

      public IUser? User => Database?.User;

      public event EventHandler? SessionChanged;

      public void StartSession(IDatabase database)
      {
         ArgumentNullException.ThrowIfNull(database);
         Database = database;
         SessionChanged?.Invoke(this, EventArgs.Empty);
      }

      public void EndSession(bool closeDatabase = true)
      {
         if (Database is null)
         {
            return;
         }

         if (closeDatabase)
         {
            Database.Close();
         }

         Database = null;
         SessionChanged?.Invoke(this, EventArgs.Empty);
      }
   }
}
