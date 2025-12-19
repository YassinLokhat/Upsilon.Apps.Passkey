using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class Change
   {
      public long Index { get; set; } = long.MaxValue;
      public LogEventType ActionType { get; set; } = LogEventType.None;
      public string ItemId { get; set; } = string.Empty;
      public string FieldName { get; set; } = string.Empty;
      public string? OldValue { get; set; } = null;
      public string NewValue { get; set; } = string.Empty;
   }
}