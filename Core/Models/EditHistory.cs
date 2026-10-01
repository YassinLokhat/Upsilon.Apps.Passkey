using System.ComponentModel;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// Session-scoped compensating undo/redo stack. Entries mirror autosave
   /// <see cref="Change"/> payloads (including secret-bearing JSON). Never
   /// persisted; cleared on vault close.
   /// </summary>
   internal sealed class EditHistory : IEditHistory
   {
      internal const int MaxDepth = 50;

      // Serializes stack mutations across concurrent UI/editor threads.
      // Never hold across Host.ApplyChange / RecordReplay (those re-enter Record).
      private readonly Lock _gate = new();
      private readonly Stack<HistoryEntry> _undo = new();
      private readonly Stack<HistoryEntry> _redo = new();
      private bool _suppressRecording;

      internal IEditHistoryHost Host
      {
         get => field ?? throw new NullValueException(nameof(Host));
         set;
      }

      public bool CanUndo
      {
         get
         {
            lock (_gate)
            {
               return _undo.Count > 0;
            }
         }
      }

      public bool CanRedo
      {
         get
         {
            lock (_gate)
            {
               return _redo.Count > 0;
            }
         }
      }

      public event EventHandler? HistoryChanged;

      public void Undo()
      {
         HistoryEntry entry;

         lock (_gate)
         {
            if (_undo.Count == 0)
            {
               return;
            }

            entry = _undo.Pop();
            _suppressRecording = true;
         }

         try
         {
            _apply(entry, undo: true);
         }
         finally
         {
            lock (_gate)
            {
               _suppressRecording = false;
               _redo.Push(entry);
            }
         }

         HistoryChanged?.Invoke(this, EventArgs.Empty);
      }

      public void Redo()
      {
         HistoryEntry entry;

         lock (_gate)
         {
            if (_redo.Count == 0)
            {
               return;
            }

            entry = _redo.Pop();
            _suppressRecording = true;
         }

         try
         {
            _apply(entry, undo: false);
         }
         finally
         {
            lock (_gate)
            {
               _suppressRecording = false;
               _undo.Push(entry);
            }
         }

         HistoryChanged?.Invoke(this, EventArgs.Empty);
      }

      public void Clear()
      {
         bool raised;

         lock (_gate)
         {
            if (_undo.Count == 0
               && _redo.Count == 0)
            {
               return;
            }

            _undo.Clear();
            _redo.Clear();
            raised = true;
         }

         if (raised)
         {
            HistoryChanged?.Invoke(this, EventArgs.Empty);
         }
      }

      /// <summary>
      /// Records a coalesced autosave change. No-op while Undo/Redo is applying,
      /// and for credential fields that must not be undoable.
      /// </summary>
      internal void Record(Change change, ChangeMergeKind mergeKind, string readableValue, bool needsReview)
      {
         bool raised = false;

         lock (_gate)
         {
            if (_suppressRecording
               || !_isUndoable(change))
            {
               return;
            }

            if (mergeKind == ChangeMergeKind.Cancelled)
            {
               _dropMatchingUndo(change);
               _redo.Clear();
               raised = true;
            }
            else if (mergeKind == ChangeMergeKind.Recorded)
            {
               if (change.ActionType == ActivityEventType.ItemUpdated
                  && _undo.Count > 0)
               {
                  HistoryEntry top = _undo.Peek();

                  if (top.ActionType == ActivityEventType.ItemUpdated
                     && top.ItemId == change.ItemId
                     && top.FieldName == change.FieldName)
                  {
                     _ = _undo.Pop();
                  }
               }

               _undo.Push(HistoryEntry.FromChange(change, readableValue, needsReview));
               _redo.Clear();
               _trimToMaxDepth();
               raised = true;
            }
         }

         if (raised)
         {
            HistoryChanged?.Invoke(this, EventArgs.Empty);
         }
      }

      private void _apply(HistoryEntry entry, bool undo)
      {
         Change change = entry.ToApplyChange(undo);
         string readableValue = _readableForReplay(change, entry);

         // Delete must record activity while the item is still in the graph
         // so ResolveActivityNames can see it; then Apply removes it.
         if (change.ActionType == ActivityEventType.ItemDeleted)
         {
            Host.RecordReplay(change, readableValue, entry.NeedsReview);
            Host.ApplyChange(change);
         }
         else
         {
            Host.ApplyChange(change);
            Host.RecordReplay(change, readableValue, entry.NeedsReview);
         }
      }

      // Caller must hold _gate.
      private void _dropMatchingUndo(Change change)
      {
         if (_undo.Count == 0
            || change.ActionType != ActivityEventType.ItemUpdated)
         {
            return;
         }

         HistoryEntry top = _undo.Peek();

         if (top.ActionType == ActivityEventType.ItemUpdated
            && top.ItemId == change.ItemId
            && top.FieldName == change.FieldName)
         {
            _ = _undo.Pop();
         }
      }

      // Caller must hold _gate.
      private void _trimToMaxDepth()
      {
         if (_undo.Count <= MaxDepth)
         {
            return;
         }

         // Stack enumerates newest-first; keep the newest MaxDepth entries.
         HistoryEntry[] newestFirst = [.. _undo];
         _undo.Clear();

         foreach (HistoryEntry entry in newestFirst.Take(MaxDepth).Reverse())
         {
            _undo.Push(entry);
         }
      }

      private static string _readableForReplay(Change change, HistoryEntry entry)
      {
         if (change.FieldName is nameof(Account.Password) or nameof(User.Passkeys))
         {
            return string.Empty;
         }

         if (change.ActionType is ActivityEventType.ItemAdded or ActivityEventType.ItemDeleted)
         {
            return entry.AfterReadable;
         }

         // Field updates: avoid echoing JSON/secrets into activity FieldValue.
         return string.Empty;
      }

      private static bool _isUndoable(Change change)
      {
         if (change.ActionType == ActivityEventType.ItemUpdated
            && change.ItemId.Length > 0
            && change.ItemId[0] == 'U'
            && change.FieldName is nameof(User.Username) or nameof(User.Passkeys))
         {
            return false;
         }

         // AddAccount seeds Password with empty OldValue + JSON string NewValue.
         // Real commits serialize the Passwords dictionary as an object ({...}).
         if (change.ActionType == ActivityEventType.ItemUpdated
            && change.FieldName == nameof(Account.Password)
            && _isEmptyPasswordSeedOldValue(change.OldValue)
            && change.NewValue.Length > 0
            && change.NewValue[0] == '"')
         {
            return false;
         }

         return change.ActionType is ActivityEventType.ItemUpdated
            or ActivityEventType.ItemAdded
            or ActivityEventType.ItemDeleted;
      }

      internal static bool IsEmptyPasswordSeedOldValue(string? oldValue)
         => _isEmptyPasswordSeedOldValue(oldValue);

      private static bool _isEmptyPasswordSeedOldValue(string? oldValue)
         => oldValue is null
            || oldValue.Length == 0
            || oldValue is "\"\"" or "null" or "{}";

      private sealed class HistoryEntry
      {
         public required ActivityEventType ActionType { get; init; }
         public required string ItemId { get; init; }
         public required string FieldName { get; init; }
         public string? BeforeJson { get; init; }
         public required string AfterJson { get; init; }
         public required string AfterReadable { get; init; }
         public required bool NeedsReview { get; init; }

         public static HistoryEntry FromChange(Change change, string readableValue, bool needsReview) => new()
         {
            ActionType = change.ActionType,
            ItemId = change.ItemId,
            FieldName = change.FieldName,
            BeforeJson = change.OldValue,
            AfterJson = change.NewValue,
            AfterReadable = change.FieldName is nameof(Account.Password) or nameof(User.Passkeys)
               ? string.Empty
               : readableValue,
            NeedsReview = needsReview,
         };

         public Change ToApplyChange(bool undo)
         {
            return ActionType switch
            {
               ActivityEventType.ItemUpdated => new Change
               {
                  ActionType = ActivityEventType.ItemUpdated,
                  ItemId = ItemId,
                  FieldName = FieldName,
                  OldValue = undo ? AfterJson : BeforeJson,
                  NewValue = undo
                     ? BeforeJson ?? string.Empty
                     : AfterJson,
               },
               ActivityEventType.ItemAdded => new Change
               {
                  ActionType = undo ? ActivityEventType.ItemDeleted : ActivityEventType.ItemAdded,
                  ItemId = ItemId,
                  FieldName = string.Empty,
                  OldValue = null,
                  NewValue = AfterJson,
               },
               ActivityEventType.ItemDeleted => new Change
               {
                  ActionType = undo ? ActivityEventType.ItemAdded : ActivityEventType.ItemDeleted,
                  ItemId = ItemId,
                  FieldName = string.Empty,
                  OldValue = null,
                  NewValue = AfterJson,
               },
               _ => throw new InvalidEnumArgumentException(nameof(ActionType), (int)ActionType, typeof(ActivityEventType)),
            };
         }
      }
   }

   internal enum ChangeMergeKind
   {
      Recorded,
      Cancelled,
   }

   /// <summary>
   /// Narrow surface EditHistory needs from Database (avoids cs/coupled-types).
   /// </summary>
   internal interface IEditHistoryHost
   {
      void ApplyChange(Change change);

      /// <summary>
      /// Syncs autosave dirty state and activity after a history Apply, without
      /// mutating the live graph again.
      /// </summary>
      void RecordReplay(Change change, string readableValue, bool needsReview);
   }
}
