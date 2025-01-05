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
      bool Login(string passkey);
      void Close();
   }
}