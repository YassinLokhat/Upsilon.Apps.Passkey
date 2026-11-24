namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>An encrypted vault copy kept because the source changed elsewhere.</summary>
   internal sealed record ConflictCopy(string Id, string FileName, DateTime CreatedUtc);

   /// <summary>
   /// Conflict copies kept in app-private storage by hosts whose vault sources
   /// cannot write side by side with the original (Android SAF). Users move them
   /// out through <see cref="IFileExchange"/>; they are deleted once saved.
   /// </summary>
   internal interface IConflictCopyStore
   {
      IReadOnlyList<ConflictCopy> List();

      Stream OpenRead(ConflictCopy copy);

      void Delete(ConflictCopy copy);
   }
}
