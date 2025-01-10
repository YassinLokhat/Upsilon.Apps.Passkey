using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.PassKey.Core.Events;
using Upsilon.Apps.PassKey.Core.Interfaces;

namespace Upsilon.Apps.Passkey.Core.Interfaces
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
      /// Create a new user database and returns the database.
      /// After creating, the User should be loaded with the Login method.
      /// </summary>
      /// <param name="cryptographicCenter">An implementation of the cryptographic center.</param>
      /// <param name="serializationCenter">An implementation of the serialization center.</param>
      /// <param name="databaseFile">The path to the database file.</param>
      /// <param name="autoSaveFile">The path to the autosave file.</param>
      /// <param name="logFile">The path to the log file.</param>
      /// <param name="username">The username.</param>
      /// <param name="passkeys">The passkeys.</param>
      /// <returns>The database created.</returns>
      static IDatabase Create(ICryptographicCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         string username,
         string[] passkeys)
         => Database.Create(cryptographicCenter,
            serializationCenter,
            databaseFile,
            autoSaveFile,
            logFile,
            username,
            passkeys);

      /// <summary>
      /// Open an user and returns the database.
      /// After opening, the User should be loaded with the Login method.
      /// </summary>
      /// <param name="cryptographicCenter">An implementation of the cryptographic center.</param>
      /// <param name="serializationCenter">An implementation of the serialization center.</param>
      /// <param name="databaseFile">The path to the database file.</param>
      /// <param name="autoSaveFile">The path to the autosave file.</param>
      /// <param name="logFile">The path to the log file.</param>
      /// <param name="username">The username.</param>
      /// <param name="autoSaveHandler">The event handler for Auto-save merge behavior.</param>
      /// <returns>The database opened.</returns>
      static IDatabase Open(ICryptographicCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         string username,
         EventHandler<AutoSaveDetectedEventArgs>? autoSaveHandler = null)
         => Database.Open(cryptographicCenter,
            serializationCenter,
            databaseFile,
            autoSaveFile,
            logFile,
            username,
            autoSaveHandler);
   }
}