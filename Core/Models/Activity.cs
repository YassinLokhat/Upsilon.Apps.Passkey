using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// One audit-log row. Ciphertext is RSA-hybrid; plaintext lives only after login.
   /// </summary>
   internal sealed class Activity : IActivity
   {
      #region IActivity interface

      public DateTime DateTime => new(DateTimeTicks);

      public string ItemId { get; } = string.Empty;

      public string? ItemName { get; set; }

      public string? FieldName { get; set; }

      public string? FieldValue { get; set; }

      public string? ParentName { get; set; }

      public ActivityEventType EventType { get; set; } = ActivityEventType.None;

      public bool NeedsReview { get; set; } = true;

      #endregion

      public long DateTimeTicks { get; set; }

      public Activity(long dateTimeTicks, string itemId, string itemName, string? fieldName, string? fieldValue, string? parentName, ActivityEventType eventType, bool needsReview)
      {
         DateTimeTicks = dateTimeTicks;
         ItemId = itemId;
         ItemName = itemName;
         FieldName = fieldName;
         FieldValue = fieldValue;
         ParentName = parentName;
         EventType = eventType;
         NeedsReview = needsReview;
      }

      public Activity(string activity)
      {
         string[] info = activity.Split('|');

         if (info.Length > 0)
         {
            DateTimeTicks = Convert.ToInt64(info[0], 16);
         }

         if (info.Length > 1)
         {
            ItemId = info[1];
         }

         if (info.Length > 2)
         {
            ItemName = info[2];
         }

         if (info.Length > 3)
         {
            FieldName = !string.IsNullOrEmpty(info[3]) ? info[3] : null;
         }

         if (info.Length > 4)
         {
            FieldValue = !string.IsNullOrEmpty(info[4]) ? info[4] : null;
         }

         if (info.Length > 5)
         {
            ParentName = !string.IsNullOrEmpty(info[5]) ? info[5] : null;
         }

         if (info.Length > 6
            && byte.TryParse(info[6], out byte eventType))
         {
            EventType = (ActivityEventType)eventType;
         }

         if (info.Length > 7)
         {
            NeedsReview = !string.IsNullOrEmpty(info[7]);
         }
      }

      /// <summary>
      /// Persistence wire format: ticks|itemId|eventType|needsReview|data… with
      /// <c>|</c> escaped as <c>\|</c> inside data. Numeric <see cref="EventType"/>
      /// values are a contract — do not renumber the enum.
      /// </summary>
      public override string ToString()
         => $"{DateTimeTicks:X}" +
            $"|{ItemId}" +
            $"|{ItemName}" +
            $"|{FieldName}" +
            $"|{FieldValue}" +
            $"|{ParentName}" +
            $"|{(int)EventType}" +
            $"|{(NeedsReview ? "1" : "")}";
   }
}
