using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// One unsaved field edit (or add/delete) waiting in the autosave ZIP entry.
   /// <see cref="Index"/> is a timestamp used to replay changes in order;
   /// <see cref="long.MaxValue"/> means "not yet sequenced".
   /// </summary>
   internal sealed class Change
   {
      public long Index { get; set; } = long.MaxValue;
      public ActivityEventType ActionType { get; set; } = ActivityEventType.None;
      public string ItemId { get; set; } = string.Empty;
      public string FieldName { get; set; } = string.Empty;
      public string? OldValue { get; set; }
      public string NewValue { get; set; } = string.Empty;
   }
}
