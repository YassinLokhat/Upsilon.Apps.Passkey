using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// Narrow session surface for <see cref="User"/> (and items that reach the
   /// vault through the user). Keeps <c>User</c> from digging into
   /// <see cref="Database"/> members (CodeQL <c>cs/coupled-types</c>).
   /// </summary>
   internal interface IUserHost
   {
      IDatabase AsDatabase { get; }

      /// <summary>
      /// Marks vault activity (resets the inactivity timer) and returns
      /// <paramref name="value"/>.
      /// </summary>
      T Touch<T>(T value);

      AutoSave AutoSave { get; }

      ISerializationCenter SerializationCenter { get; }

      ICryptographyCenter CryptographyCenter { get; }

      IClipboardManager ClipboardManager { get; }

      string Username { get; }

      bool HasPendingChanges(string itemId);

      void AddActivity(string itemId,
         ActivityEventType eventType,
         string[] data,
         bool needsReview);

      void PersistActivityLog(bool rebuildStringActivities);

      void CloseOnSessionTimeout();
   }
}
