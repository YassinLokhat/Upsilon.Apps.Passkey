using Upsilon.Apps.Passkey.GUI.Presentation.Alerts;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;

using AppServices = Upsilon.Apps.Passkey.GUI.WPF.Services.AppServices;
using IDialogService = Upsilon.Apps.Passkey.GUI.WPF.Services.IDialogService;
using INavigationService = Upsilon.Apps.Passkey.GUI.WPF.Services.INavigationService;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui.Fakes
{
   internal sealed class FakeSessionService : ISessionService
   {
      public IDatabase? Database { get; set; }

      public IUser? User => Database?.User;

      public AlertBroker Alerts { get; } = new(static () => HostSecurityIssue.None);

      public event EventHandler? SessionChanged;

      public event EventHandler<VaultWriteBackEventArgs>? WriteBackCompleted
      {
         add { }
         remove { }
      }

      public void TrackVaultSource(IDatabase database, IVaultSource source)
      {
      }

      public Task<bool> FlushWriteBacksAsync(TimeSpan timeout) => Task.FromResult(true);

      public void StartSession(IDatabase database)
      {
         ArgumentNullException.ThrowIfNull(database);
         Database = database;
         Alerts.Attach(database);
         SessionChanged?.Invoke(this, EventArgs.Empty);
      }

      public void EndSession(bool closeDatabase = true)
      {
         if (Database is null)
         {
            return;
         }

         Alerts.Detach();

         if (closeDatabase)
         {
            Database.Close();
         }

         Database = null;
         SessionChanged?.Invoke(this, EventArgs.Empty);
      }

      public void ApplySessionLanguage()
      {
      }

      public void ApplySessionTheme()
      {
      }
   }
}
