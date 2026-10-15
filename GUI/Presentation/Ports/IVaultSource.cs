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

      /// <summary>Check out / refresh the working copy from the durable source.</summary>
      Task CheckoutAsync(CancellationToken cancellationToken = default);

      /// <summary>
      /// Write the working copy back to the durable source after Save/Close.
      /// Returns a conflict result when the source changed since checkout.
      /// </summary>
      Task<VaultWriteBackResult> WriteBackAsync(CancellationToken cancellationToken = default);
   }

   internal enum VaultWriteBackResult
   {
      Written = 0,
      Conflict = 1,
      Skipped = 2,
      Failed = 3,
   }

   internal interface IVaultSourceFactory
   {
      /// <summary>Open an existing vault by filesystem path (Windows / tests).</summary>
      Task<IVaultSource> OpenPathAsync(string path, CancellationToken cancellationToken = default);

      /// <summary>Create a new vault path under the default database directory.</summary>
      Task<IVaultSource> CreatePathAsync(string path, CancellationToken cancellationToken = default);
   }
}
