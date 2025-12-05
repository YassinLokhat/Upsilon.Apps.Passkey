using Upsilon.Apps.Passkey.Interfaces.Events;

namespace Upsilon.Apps.Passkey.Interfaces
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
      /// The path to the autosave file.
      /// </summary>
      string AutoSaveFile { get; set; }

      /// <summary>
      /// The path to the log file.
      /// </summary>
      string LogFile { get; set; }

      /// <summary>
      /// The user loaded.
      /// </summary>
      IUser? User { get; }

      /// <summary>
      /// The number of seconds left before the session ended.
      /// </summary>
      int? SessionLeftTime { get; }

      /// <summary>
      /// The logs.
      /// </summary>
      ILog[]? Logs { get; }

      /// <summary>
      /// The warnings detected.
      /// </summary>
      IWarning[]? Warnings { get; }

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
      /// Occurs when a warning is detected.
      /// </summary>
      event EventHandler<WarningDetectedEventArgs>? WarningDetected;

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
      /// Try to load the current user.
      /// </summary>
      /// <param name="passkey">The current passkey.</param>
      /// <returns>The loaded user.</returns>
      IUser? Login(string passkey);

      /// <summary>
      /// Save the current user to database file.
      /// The User must be loaded, else it will throw a NullReferenceException.
      /// </summary>
      void Save();

      /// <summary>
      /// Delete the current user with all its files.
      /// The User must be loaded, else it will throw a NullReferenceException.
      /// </summary>
      void Delete();

      /// <summary>
      /// Close the current user and database.
      /// </summary>
      void Close();

      /// <summary>
      /// Check if the database has changed.
      /// </summary>
      /// <returns>True if the database changed, False else.</returns>
      bool HasChanged();

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
      /// Export services and accounts to a file.
      /// </summary>
      /// <param name="filePath">The file path.</param>
      /// <returns>True if the export succeded, False else.</returns>
      bool ExportToFile(string filePath);
   }
}