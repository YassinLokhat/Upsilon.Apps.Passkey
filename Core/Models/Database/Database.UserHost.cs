using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   public sealed partial class Database : IUserHost
   {
      IDatabase IUserHost.AsDatabase => this;

      T IUserHost.Touch<T>(T value) => Get(value);

      AutoSave IUserHost.AutoSave => AutoSave;

      ISerializationCenter IUserHost.SerializationCenter => SerializationCenter;

      ICryptographyCenter IUserHost.CryptographyCenter => CryptographyCenter;

      IClipboardManager IUserHost.ClipboardManager => ClipboardManager;

      string IUserHost.Username => Username;

      bool IUserHost.HasPendingChanges(string itemId) => HasChanged(itemId);

      void IUserHost.AddActivity(string itemId,
         string? username,
         string? serviceName,
         string? accountName,
         string? fieldName,
         string? fieldValue,
         string? parentName,
         ActivityEventType eventType,
         bool needsReview)
         => ActivityCenter.AddActivity(itemId,
            username,
            serviceName,
            accountName,
            fieldName,
            fieldValue,
            parentName,
            eventType,
            needsReview);

      void IUserHost.PersistActivityLog(bool rebuildStringActivities)
         => ActivityCenter.Save(rebuildStringActivities);

      void IUserHost.CloseOnSessionTimeout()
         => Close(logCloseEvent: true, loginTimeoutReached: true);
   }
}
