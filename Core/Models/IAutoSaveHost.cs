using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// Narrow surface AutoSave needs from its owning vault. Keeps the association
   /// unidirectional (<c>Database</c> → <c>AutoSave</c>) so AutoSave does not
   /// dig into Database members (CodeQL <c>cs/coupled-types</c>).
   /// </summary>
   internal interface IAutoSaveHost
   {
      ISerializationCenter SerializationCenter { get; }

      void ResolveActivityNames(string itemId,
         ActivityEventType action,
         out string itemName,
         out string parentName);

      void AddActivity(string itemId,
         ActivityEventType eventType,
         string[] data,
         bool needsReview);

      /// <summary>
      /// Drops the unsealed <see cref="ActivityEventType.ItemUpdated"/> row for
      /// <paramref name="itemId"/>/<paramref name="fieldName"/> when a field edit
      /// is fully reverted (same outcome as clearing the coalesced Change).
      /// </summary>
      void CancelPendingItemUpdatedActivity(string itemId, string fieldName);

      void ApplyChange(Change change);

      bool AutoSaveEntryExists();

      void DeleteAutoSaveEntry();

      void SaveAutoSave(AutoSave autoSave);
   }
}
