﻿using Upsilon.Apps.Passkey.Core.Models;

namespace Upsilon.Apps.Passkey.Core.Interfaces
{
   public interface IDatabase : IDisposable
   {
      string DatabaseFile { get; set; }
      string AutoSaveFile { get; set; }
      string LogFile { get; set; }
      IUser? User { get; }

      void Delete();
      void Save();
      void HandleAutoSave(bool mergeAutoSave);
      IUser? Login(string passkey);
      void Close();

      static IDatabase Create(string databaseFile, string autoSaveFile, string logFile, string username, string[] passkeys)
         => Database.Create(databaseFile, autoSaveFile, logFile, username, passkeys);

      static IDatabase Open(string databaseFile, string autoSaveFile, string logFile, string username)
         => Database.Open(databaseFile, autoSaveFile, logFile, username);
   }
}