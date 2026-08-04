using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Represent a database.
   /// </summary>
   public interface IDatabase : IDisposable
   {
      /// <summary>
      /// The path to the database file.
      /// </summary>
      string DatabaseFile { get; set; }

      /// <summary>
      /// The user loaded.
      /// </summary>
      IUser? User { get; }

      /// <summary>
      /// The number of seconds left before the session ended.
      /// </summary>
      int? SessionLeftTime { get; }

      /// <summary>
      /// The activities.
      /// </summary>
      IEnumerable<IActivity>? Activities { get; }

      /// <summary>
      /// The warnings detected.
      /// </summary>
      IEnumerable<IWarning>? Warnings { get; }

      /// <summary>
      /// The serialization center implementation.
      /// </summary>
      ISerializationCenter SerializationCenter { get; }

      /// <summary>
      /// The cryptographic center implementation.
      /// </summary>
      ICryptographyCenter CryptographyCenter { get; }

      /// <summary>
      /// The password factory implementation.
      /// </summary>
      IPasswordFactory PasswordFactory { get; }

      /// <summary>
      /// The OS specific Clipboard manager implementation.
      /// </summary>
      IClipboardManager ClipboardManager { get; }

      /// <summary>
      /// Occurs when a warning is detected.
      /// </summary>
      event EventHandler<WarningsUpdatedEventArgs>? WarningsUpdated;

      /// <summary>
      /// Occurs when an autosave is detected.
      /// </summary>
      event EventHandler<AutoSaveDetectedEventArgs>? AutoSaveDetected;

      /// <summary>
      /// Occurs when the database is saved.
      /// </summary>
      event EventHandler? DatabaseSaved;

      /// <summary>
      /// Occurs when an database is closed.
      /// </summary>
      event EventHandler<LogoutEventArgs>? DatabaseClosed;

      /// <summary>
      /// Try to load the current user by appending one passkey to the progressive
      /// login stack. There is no rollback: a wrong passkey poisons the current
      /// open session until <see cref="Close"/>, which is intentional anti-brute-force
      /// friction (see SECURITY.md).
      /// </summary>
      /// <param name="passkey">The current passkey.</param>
      /// <returns>The loaded user, or <see langword="null"/> if login is incomplete or failed.</returns>
      IUser? Login(string passkey);

      /// <summary>
      /// Same as <see cref="Login"/>, but handed to a worker thread so the caller
      /// stays responsive. Stretching a passkey costs hundreds of milliseconds by
      /// design, which is far too long to spend on a UI thread.
      /// </summary>
      /// <remarks>
      /// Every event raised while loading (<see cref="AutoSaveDetected"/> in
      /// particular) is invoked from that worker thread, so a handler touching UI
      /// state has to marshal back to its own thread.
      /// </remarks>
      /// <param name="passkey">The current passkey.</param>
      /// <param name="cancellationToken">Abandons the attempt while it is still queued.</param>
      /// <returns>The loaded user, or <see langword="null"/> if login is incomplete or failed.</returns>
      Task<IUser?> LoginAsync(string passkey, CancellationToken cancellationToken = default);

      /// <summary>
      /// Save the current user to database file.
      /// The User must be loaded, else it will throw a NullValueException.
      /// </summary>
      void Save();

      /// <summary>
      /// Same as <see cref="Save"/>, but handed to a worker thread so the caller
      /// stays responsive.
      /// </summary>
      /// <remarks>
      /// <see cref="DatabaseSaved"/> and <see cref="WarningsUpdated"/> are raised
      /// from that worker thread.
      /// </remarks>
      /// <param name="cancellationToken">Abandons the save while it is still queued.</param>
      Task SaveAsync(CancellationToken cancellationToken = default);

      /// <summary>
      /// Delete the current user with all its files.
      /// The User must be loaded, else it will throw a NullValueException.
      /// </summary>
      void Delete();

      /// <summary>
      /// Close the current user and database.
      /// </summary>
      void Close();

      /// <summary>
      /// Check if the given item has changed.
      /// </summary>
      /// <param name="itemId">The item id to check.</param>
      /// <returns>True if the item changed, False else.</returns>
      bool HasChanged(string itemId);

      /// <summary>
      /// Check if the field of the given item has changed.
      /// </summary>
      /// <param name="itemId">The item id to check.</param>
      /// <param name="fieldName">The field name to check.</param>
      /// <returns>True if the field changed, False else.</returns>
      bool HasChanged(string itemId, string fieldName);

      /// <summary>
      /// Import services and/or accounts from a file.
      /// </summary>
      /// <param name="filePath">The file path.</param>
      /// <returns>True if the import succeded, False else.</returns>
      bool ImportFromFile(string filePath);

      /// <summary>
      /// Same as <see cref="ImportFromFile"/>, but handed to a worker thread so
      /// the caller stays responsive.
      /// </summary>
      /// <param name="filePath">The file path.</param>
      /// <param name="cancellationToken">Abandons the import while it is still queued.</param>
      /// <returns>True if the import succeded, False else.</returns>
      Task<bool> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default);

      /// <summary>
      /// Export services and accounts to a file.
      /// </summary>
      /// <param name="filePath">The file path.</param>
      /// <returns>True if the export succeded, False else.</returns>
      bool ExportToFile(string filePath);

      /// <summary>
      /// Same as <see cref="ExportToFile"/>, but handed to a worker thread so the
      /// caller stays responsive.
      /// </summary>
      /// <param name="filePath">The file path.</param>
      /// <param name="cancellationToken">Abandons the export while it is still queued.</param>
      /// <returns>True if the export succeded, False else.</returns>
      Task<bool> ExportToFileAsync(string filePath, CancellationToken cancellationToken = default);
   }
}
