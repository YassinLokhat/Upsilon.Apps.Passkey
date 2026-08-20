using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// Debounced, onion-encrypted list of unsaved <see cref="Change"/>s. Lock
   /// order when nested: AutoSave → ActivityCenter → FileLocker.
   /// </summary>
   internal sealed class AutoSave : IDisposable
   {
      internal IAutoSaveHost Host
      {
         get => field ?? throw new NullValueException(nameof(Host));
         set;
      }

      // Serialized to/from the autosave ZIP entry. All in-memory reads and writes
      // go through _gate so a deferred flush cannot enumerate a torn dictionary
      // while the UI thread mutates it.
      public Dictionary<string, List<Change>> Changes { get; set; } = [];

      // Serializes access to Changes across the UI thread (edits) and the
      // DeferredPersistence timer / Flush path (disk writes). Lock order when
      // nested with ActivityCenter is always AutoSave → ActivityCenter →
      // FileLocker; never the reverse.
      private readonly Lock _gate = new();

      // Field edits often arrive in bursts (typing, multi-property forms). Writing
      // the onion-encrypted ZIP entry on every keystroke is the dominant I/O cost
      // of an interactive session; coalesce them into one write after a short idle.
      private readonly DeferredPersistence _deferred;

      public AutoSave()
      {
         _deferred = new DeferredPersistence(_writeToDisk);
      }

      internal T UpdateValue<T>(string itemId,
         string fieldName,
         bool needsReview,
         T oldValue,
         T newValue,
         string readableValue) where T : notnull
      {
         if (Host.SerializationCenter.AreDifferent(oldValue, newValue))
         {
            _addChange(itemId,
               fieldName,
               oldValue.SerializeWith(Host.SerializationCenter),
               newValue.SerializeWith(Host.SerializationCenter),
               readableValue,
               needsReview,
               ActivityEventType.ItemUpdated);
         }

         return newValue;
      }

      internal T AddValue<T>(string itemId,
         string readableValue,
         bool needsReview,
         T value) where T : notnull
      {
         _addChange(itemId, string.Empty, value.SerializeWith(Host.SerializationCenter), readableValue, needsReview, ActivityEventType.ItemAdded);

         return value;
      }

      internal T DeleteValue<T>(string itemId,
         string readableValue,
         bool needsReview,
         T value) where T : notnull
      {
         _addChange(itemId, string.Empty, value.SerializeWith(Host.SerializationCenter), readableValue, needsReview, ActivityEventType.ItemDeleted);

         return value;
      }

      private void _addChange(string itemId,
         string fieldName,
         string newValue,
         string readableValue,
         bool needsReview,
         ActivityEventType action)
      {
         _addChange(itemId,
            fieldName,
            null,
            newValue,
            readableValue,
            needsReview,
            action);
      }

      private void _addChange(string itemId,
         string fieldName,
         string? oldValue,
         string newValue,
         string readableValue,
         bool needsReview,
         ActivityEventType action)
      {
         string changeKey = $"{itemId}\t{fieldName}";

         Change currentChange = new()
         {
            Index = DateTime.Now.Ticks,
            ActionType = action,
            ItemId = itemId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
         };

         lock (_gate)
         {
            if (!Changes.ContainsKey(changeKey))
            {
               Changes[changeKey] = [];
            }

            _mergeChanges(changeKey, currentChange);
         }

         // Persist later; the in-memory Changes dictionary is already updated so
         // HasChanged / ApplyChanges stay correct without waiting for the flush.
         // Schedule outside the lock: DeferredPersistence has its own gate, and
         // holding _gate across Schedule would nest locks needlessly.
         _deferred.Schedule();

         Host.ResolveActivityNames(itemId, action, out string itemName, out string parentName);

         string[] data = [itemName, fieldName, readableValue];

         if (!string.IsNullOrEmpty(parentName))
         {
            data = [.. data, parentName];
         }

         // ActivityCenter takes its own gate; we deliberately do not hold _gate
         // here so RSA encrypt + activity insert cannot stall an autosave flush.
         Host.AddActivity(itemId: itemId,
            eventType: action,
            data,
            needsReview);
      }

      // Caller must hold _gate.
      private void _mergeChanges(string changeKey, Change currentChange)
      {
         Change? lastUpdate = Changes[changeKey].LastOrDefault(x => x.ActionType == ActivityEventType.ItemUpdated);

         if (currentChange.ActionType != ActivityEventType.ItemUpdated
            || lastUpdate is null)
         {
            Changes[changeKey].Add(currentChange);
            return;
         }

         _ = Changes[changeKey].Remove(lastUpdate);
         currentChange.OldValue = lastUpdate.OldValue;

         if (currentChange.OldValue != currentChange.NewValue)
         {
            Changes[changeKey].Add(currentChange);
         }
         else if (Changes[changeKey].Count == 0)
         {
            _ = Changes.Remove(changeKey);
         }
      }

      internal void ApplyChanges(bool deleteFile)
      {
         List<Change> changes;

         lock (_gate)
         {
            changes = [.. Changes.Values.SelectMany(x => x).OrderBy(x => x.Index)];
         }

         foreach (Change change in changes)
         {
            Host.ApplyChange(change);
         }

         if (deleteFile)
         {
            Clear(deleteFile: true);
         }
      }

      internal bool Any() => Any(string.Empty);

      internal bool Any(string itemId)
      {
         lock (_gate)
         {
            return Changes.Any(x => x.Key.StartsWith(itemId, StringComparison.Ordinal));
         }
      }

      internal bool Any(string itemId, string fieldName)
      {
         lock (_gate)
         {
            return Changes.Any(x => x.Key == $"{itemId}\t{fieldName}");
         }
      }

      /// <summary>
      /// Forces any debounced autosave write to disk. Must be called before Close
      /// when <see cref="Any"/> is true so the recovery file survives the session.
      /// </summary>
      internal void Flush() => _deferred.Flush();

      internal void Clear(bool deleteFile)
      {
         // Cancel first so a timer that has not yet entered _writeToDisk drops
         // its dirty flag; _writeToDisk still re-checks emptiness under _gate.
         _deferred.Cancel();

         lock (_gate)
         {
            Changes.Clear();
         }

         if (deleteFile
            && Host.AutoSaveEntryExists())
         {
            Host.DeleteAutoSaveEntry();
         }
      }

      public void Dispose() => _deferred.Dispose();

      private void _writeToDisk()
      {
         // Hold _gate across serialize + ZIP write so Changes cannot be mutated
         // mid-enumeration. FileLocker has its own re-entrant gate; lock order is
         // AutoSave → FileLocker.
         lock (_gate)
         {
            // A timer flush can race with Clear (e.g. during Save). If the pending
            // changes were discarded, do not recreate the autosave ZIP entry.
            if (Changes.Count == 0)
            {
               return;
            }

            Host.SaveAutoSave(this);
         }
      }
   }
}
