using Upsilon.Apps.PassKey.Core.Events;

namespace Upsilon.Apps.PassKey.Core.Interfaces
{
   /// <summary>
   /// Represent a database.
   /// </summary>
   public interface IDatabase : IDisposable
   {
      /// <summary>
      /// The path to the database file.
      /// </summary>
      string DatabaseFile { get; }

      /// <summary>
      /// The path to the autosave file.
      /// </summary>
      string AutoSaveFile { get; }

      /// <summary>
      /// The path to the log file.
      /// </summary>
      string LogFile { get; }

      /// <summary>
      /// The user loaded.
      /// </summary>
      IUser? User { get; }

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
      /// Create a new user database and returns the database.
      /// After creating, the User should be loaded with the Login method.
      /// </summary>
      /// <param name="cryptographicCenter">An implementation of the cryptographic center.</param>
      /// <param name="serializationCenter">An implementation of the serialization center.</param>
      /// <param name="passwordFactory">An implementation of the password factory.</param>
      /// <param name="databaseFile">The path to the database file.</param>
      /// <param name="autoSaveFile">The path to the autosave file.</param>
      /// <param name="logFile">The path to the log file.</param>
      /// <param name="username">The username.</param>
      /// <param name="passkeys">The passkeys.</param>
      /// <returns>The database created.</returns>
      static IDatabase Create(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         string username,
         string[] passkeys)
         => Internal.Models.Database.Create(cryptographicCenter,
            serializationCenter,
            passwordFactory,
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
      /// <param name="passwordFactory">An implementation of the password factory.</param>
      /// <param name="databaseFile">The path to the database file.</param>
      /// <param name="autoSaveFile">The path to the autosave file.</param>
      /// <param name="logFile">The path to the log file.</param>
      /// <param name="username">The username.</param>
      /// <returns>The database opened.</returns>
      static IDatabase Open(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         string username)
         => Internal.Models.Database.Open(cryptographicCenter,
            serializationCenter,
            passwordFactory,
            databaseFile,
            autoSaveFile,
            logFile,
            username);
   }
}