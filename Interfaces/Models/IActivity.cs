using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IActivity
   {
      DateTime DateTime { get; }

      string ItemId { get; }

      string? ItemName { get; }

      string? FieldName { get; }

      string? FieldValue { get; }

      string? ParentName { get; }

      ActivityEventType EventType { get; }

      bool NeedsReview { get; set; }
   }
}
