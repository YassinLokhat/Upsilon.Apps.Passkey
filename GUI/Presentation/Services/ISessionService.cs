using Upsilon.Apps.Passkey.GUI.Presentation.Alerts;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>
   /// Tracks the open <see cref="IDatabase"/> for the whole UI and owns the
   /// lifetime of its <see cref="IVaultSource"/> (write-back, then dispose).
   /// </summary>
   internal interface ISessionService
   {
      /// <summary>The active database, or <c>null</c> when nobody is logged in.</summary>
      IDatabase? Database { get; }

      /// <summary>The active user, or <c>null</c> when no database is loaded or the user is not logged in.</summary>
      IUser? User { get; }

      /// <summary>Aggregates Core + host alerts and applies notify filters.</summary>
      AlertBroker Alerts { get; }

      /// <summary>Raised on the UI thread whenever <see cref="Database"/> or <see cref="User"/> changes.</summary>
      event EventHandler? SessionChanged;

      /// <summary>Raised on the UI thread after a write-back that needs the user's attention.</summary>
      event EventHandler<VaultWriteBackEventArgs>? WriteBackCompleted;

      /// <summary>
      /// Hands <paramref name="source"/> to the session as soon as
      /// <paramref name="database"/> is opened (before login): once the
      /// database closed and released its file, the working copy is written
      /// back, then <paramref name="source"/> is disposed.
      /// </summary>
      void TrackVaultSource(IDatabase database, IVaultSource source);

      /// <summary>
      /// Registers <paramref name="database"/> as the current session. Any existing
      /// session is closed first (through the same write-back path).
      /// </summary>
      void StartSession(IDatabase database);

      /// <summary>
      /// Closes the current session, if any. Safe to call twice and from any thread.
      /// </summary>
      /// <param name="closeDatabase">
      /// When <see langword="true"/> (default), calls <see cref="IDatabase.Close"/> on the
      /// active database. Pass <see langword="false"/> when the database has already
      /// closed itself (e.g. from a <c>DatabaseClosed</c> handler) so the session is
      /// only cleared.
      /// </param>
      void EndSession(bool closeDatabase = true);

      /// <summary>
      /// Waits for pending write-backs. Returns <see langword="false"/> when
      /// <paramref name="timeout"/> elapsed first. Call before exit or when the
      /// host is about to suspend the process.
      /// </summary>
      Task<bool> FlushWriteBacksAsync(TimeSpan timeout);

      /// <summary>
      /// Runs <paramref name="operation"/> after every queued write-back, rename
      /// or delete. Checkouts go through here: a source reopened right after a
      /// logout shares its private working directory with the closing session.
      /// </summary>
      Task<T> RunSourceOperationAsync<T>(Func<Task<T>> operation);

      /// <summary>
      /// Writes pending Core changes to the working copy, then writes it back to
      /// the durable source while the session stays open (e.g. when the app is
      /// backgrounded). Runs in order with close write-backs. <c>null</c> when
      /// no tracked database is open.
      /// </summary>
      Task<VaultWriteBackResult?> FlushToSourceAsync();

      /// <summary>
      /// Deletes the active vault: Core deletes the working copy and closes, then
      /// the durable source is deleted instead of written back.
      /// </summary>
      void DeleteVault();

      /// <summary>
      /// <see cref="DeleteVault"/>, completing with whether the durable vault is
      /// gone once the source finished (a refused source delete yields <c>false</c>).
      /// </summary>
      Task<bool> DeleteVaultAsync();

      /// <summary>
      /// Ends the session, writes the vault back, then renames the durable vault
      /// to <paramref name="newFileName"/> before the source is released. The
      /// rename never overwrites and is skipped when the write-back failed.
      /// </summary>
      Task<VaultRenameOutcome> EndSessionAndRenameAsync(string newFileName);

      /// <summary>
      /// Applies the effective UI language for the current session (user override
      /// when logged in, otherwise the application language).
      /// </summary>
      void ApplySessionLanguage();

      /// <summary>
      /// Applies the effective UI theme for the current session (user override
      /// when logged in, otherwise the application theme).
      /// </summary>
      void ApplySessionTheme();
   }

   /// <param name="Renamed">The durable vault now has the requested name.</param>
   /// <param name="Location">Reopen key after the operation (new one when renamed); <c>null</c> when unknown.</param>
   internal sealed record VaultRenameOutcome(bool Renamed, VaultWriteBackResult WriteBack, string? Location);

   internal sealed class VaultWriteBackEventArgs(VaultWriteBackResult result, string message) : EventArgs
   {
      public VaultWriteBackResult Result { get; } = result;

      /// <summary>Localized, path-free status text.</summary>
      public string Message { get; } = message;
   }
}
