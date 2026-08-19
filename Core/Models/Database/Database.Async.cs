using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   public sealed partial class Database
   {
      // Everything the database does is CPU- and disk-bound: stretching passkeys,
      // peeling AES layers, re-encrypting the activity log, rewriting the ZIP.
      // There is no naturally asynchronous work to await underneath, so these
      // entry points simply hand the synchronous operation to the thread pool and
      // give the caller something to await. The point is not throughput, it is
      // that a UI thread never spends a second deriving a key.
      //
      // The operations below share mutable state (the progressive passkey stack,
      // the file itself), so they are not meant to overlap: await one before
      // starting the next. Their events are raised from the worker thread.

      public Task<IUser?> LoginAsync(string passkey, CancellationToken cancellationToken = default)
         => Task.Run(() => Login(passkey), cancellationToken);

      public Task SaveAsync(CancellationToken cancellationToken = default)
         => Task.Run(Save, cancellationToken);

      public Task<bool> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default)
         => Task.Run(() => ImportFromFile(filePath), cancellationToken);

      public Task<bool> ExportToFileAsync(string filePath, CancellationToken cancellationToken = default)
         => Task.Run(() => ExportToFile(filePath), cancellationToken);

      /// <summary>
      /// Same as <see cref="Create"/>, but handed to a worker thread so the
      /// caller stays responsive. Creating a database is the single most
      /// expensive operation of the whole application: an RSA-4096 key pair, one
      /// stretching per passkey, then a full save.
      /// </summary>
      public static Task<IDatabase> CreateAsync(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         IClipboardManager clipboardManager,
         string databaseFile,
         string username,
         string[] passkeys,
         CancellationToken cancellationToken = default)
         => Task.Run(() => Create(cryptographicCenter,
               serializationCenter,
               passwordFactory,
               clipboardManager,
               databaseFile,
               username,
               passkeys),
            cancellationToken);

      /// <summary>
      /// Same as <see cref="Open"/>, but handed to a worker thread so the caller
      /// stays responsive. Opening reads and decrypts the whole activity log,
      /// which grows with the file's history.
      /// </summary>
      public static Task<IDatabase> OpenAsync(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         IClipboardManager clipboardManager,
         string databaseFile,
         string username,
         CancellationToken cancellationToken = default)
         => Task.Run(() => Open(cryptographicCenter,
               serializationCenter,
               passwordFactory,
               clipboardManager,
               databaseFile,
               username),
            cancellationToken);
   }
}
