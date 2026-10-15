namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>
   /// Host vault file access. On Windows this is typically a direct path;
   /// on Android it is SAF + private working copy with conflict detection.
   /// Owned by the session: disposed after the final write-back, which removes
   /// the working copy once it reached the durable source.
   /// </summary>
   internal interface IVaultSource : IAsyncDisposable
   {
      /// <summary>Local filesystem path Core can open (working copy or real file).</summary>
      string WorkingPath { get; }

      /// <summary>Display name for UI (may differ from <see cref="WorkingPath"/>).</summary>
      string DisplayName { get; }

      /// <summary>
      /// Key that reopens this vault through <see cref="IVaultSourceFactory.OpenPathAsync"/>
      /// (a path, or a SAF URI string). Changes after <see cref="RenameAsync"/>.
      /// </summary>
      string Location { get; }

      /// <summary>
      /// Check out / refresh the working copy from the durable source. A working
      /// copy left by an interrupted run is recovered when the source did not
      /// change since, and set aside as a conflict copy otherwise.
      /// </summary>
      Task<VaultCheckoutResult> CheckoutAsync(CancellationToken cancellationToken = default);

      /// <summary>
      /// Write the working copy back to the durable source. Safe while Core
      /// still has the vault open (call <c>IDatabase.FlushPendingChanges</c>
      /// first). Returns a conflict result when the source changed since checkout.
      /// </summary>
      Task<VaultWriteBackResult> WriteBackAsync(CancellationToken cancellationToken = default);

      /// <summary>
      /// Deletes the durable vault after Core deleted the working copy. Refuses
      /// (returns <c>false</c>) when the source changed since checkout.
      /// </summary>
      Task<bool> DeleteAsync(CancellationToken cancellationToken = default);

      /// <summary>
      /// Renames the durable vault once the database is closed and written back.
      /// Never overwrites an existing vault; returns <c>false</c> when the
      /// rename is refused or unsupported, leaving the vault under its old name.
      /// </summary>
      Task<bool> RenameAsync(string newFileName, CancellationToken cancellationToken = default);
   }

   internal enum VaultCheckoutResult
   {
      Fresh = 0,

      /// <summary>Unsaved changes from an interrupted run were kept as the working copy.</summary>
      RecoveredLocalChanges = 1,

      /// <summary>An interrupted run's changes conflicted with the source and were set aside.</summary>
      StaleCopyKeptAsConflict = 2,
   }

   internal enum VaultWriteBackResult
   {
      Written = 0,
      Conflict = 1,
      Skipped = 2,
      Failed = 3,

      /// <summary>A failed write could not be undone; the local copy was kept.</summary>
      SourceDamaged = 4,
   }

   internal interface IVaultSourceFactory
   {
      /// <summary>Open an existing vault by filesystem path (Windows / tests).</summary>
      Task<IVaultSource> OpenPathAsync(string path, CancellationToken cancellationToken = default);

      /// <summary>Create a new vault path under the default database directory.</summary>
      Task<IVaultSource> CreatePathAsync(string path, CancellationToken cancellationToken = default);

      /// <summary>
      /// Lets the user choose where to create a new vault (SAF <c>CreateDocument</c>).
      /// <c>null</c> when cancelled, unsupported by the host, or when the chosen
      /// document is not empty.
      /// </summary>
      Task<IVaultSource?> CreateInteractiveAsync(string suggestedFileName, CancellationToken cancellationToken = default);
   }
}
