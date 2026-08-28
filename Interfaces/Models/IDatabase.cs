using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Open vault session: progressive login, save, import/export, warnings.
   /// </summary>
   public interface IDatabase : IDisposable
   {
      string DatabaseFile { get; set; }

      IUser? User { get; }

      /// <summary>Seconds remaining before auto-logout; <see langword="null"/> when logged out.</summary>
      int? SessionLeftTime { get; }

      IEnumerable<IActivity>? Activities { get; }

      IEnumerable<IWarning>? Warnings { get; }

      ISerializationCenter SerializationCenter { get; }

      ICryptographyCenter CryptographyCenter { get; }

      IPasswordFactory PasswordFactory { get; }

      IClipboardManager ClipboardManager { get; }

      event EventHandler<WarningsUpdatedEventArgs>? WarningsUpdated;

      event EventHandler<AutoSaveDetectedEventArgs>? AutoSaveDetected;

      event EventHandler? DatabaseSaved;

      event EventHandler<LogoutEventArgs>? DatabaseClosed;

      /// <summary>
      /// Append one passkey to the progressive login stack. There is no rollback:
      /// a wrong passkey poisons the session until <see cref="Close"/> (intentional
      /// anti-brute-force friction; see SECURITY.md).
      /// </summary>
      /// <returns>
      /// The user when login completes, or <see langword="null"/> for an incomplete
      /// onion or a wrong passkey (both caught internally).
      /// </returns>
      /// <exception cref="CorruptedSourceException">
      /// The database entry is corrupted or not a Passkey vault payload.
      /// </exception>
      IUser? Login(string passkey);

      /// <summary>
      /// Same as <see cref="Login"/> on a worker thread (PBKDF2 is too slow for the UI thread).
      /// </summary>
      /// <remarks>
      /// Events such as <see cref="AutoSaveDetected"/> are raised from that worker
      /// thread; UI handlers must marshal back.
      /// </remarks>
      Task<IUser?> LoginAsync(string passkey, CancellationToken cancellationToken = default);

      /// <summary>
      /// Persist the logged-in user. Throws <see cref="NullValueException"/> if not logged in.
      /// </summary>
      void Save();

      /// <summary>
      /// Same as <see cref="Save"/> on a worker thread.
      /// </summary>
      /// <remarks>
      /// <see cref="DatabaseSaved"/> and <see cref="WarningsUpdated"/> are raised from that thread.
      /// </remarks>
      Task SaveAsync(CancellationToken cancellationToken = default);

      /// <summary>
      /// Delete the vault file. Throws <see cref="NullValueException"/> if not logged in.
      /// </summary>
      void Delete();

      void Close();

      bool HasChanged(string itemId);

      bool HasChanged(string itemId, string fieldName);

      /// <summary>
      /// Import from <c>.json</c> or <c>.csv</c> (comma- or tab-delimited). Requires a logged-in user.
      /// </summary>
      bool ImportFromFile(string filePath);

      Task<bool> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default);

      /// <summary>
      /// Export to <c>.json</c> or <c>.csv</c>. Files are plaintext — see SECURITY.md.
      /// </summary>
      bool ExportToFile(string filePath);

      Task<bool> ExportToFileAsync(string filePath, CancellationToken cancellationToken = default);
   }
}
