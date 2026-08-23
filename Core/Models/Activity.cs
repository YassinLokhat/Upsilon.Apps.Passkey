using Upsilon.Apps.Passkey.Core.Utils;
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

      public ActivityEventType EventType { get; set; } = ActivityEventType.None;

      public bool NeedsReview { get; set; } = true;

      public IEnumerable<string> Data { get; set; } = [];

      #endregion

      public long DateTimeTicks { get; set; }

      public Activity(long dateTimeTicks, string itemId, ActivityEventType eventType, string[] data, bool needsReview)
      {
         DateTimeTicks = dateTimeTicks;
         ItemId = itemId;
         EventType = eventType;
         Data = data;
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

         if (info.Length > 2
            && byte.TryParse(info[2], out byte eventType))
         {
            EventType = (ActivityEventType)eventType;
         }

         if (info.Length > 3)
         {
            NeedsReview = !string.IsNullOrEmpty(info[3]);
         }

         if (info.Length > 4)
         {
            activity = string.Join("|", info[4..])
               .Replace("|", "/|", StringComparison.Ordinal)
               .Replace("\\/|", "\\|", StringComparison.Ordinal);
            info = activity.Split("/|");
            Data = [.. info.Select(x => x.Replace("\\|", "|", StringComparison.Ordinal))];
         }
      }

      /// <summary>
      /// Persistence wire format: ticks|itemId|eventType|needsReview|data… with
      /// <c>|</c> escaped as <c>\|</c> inside data. Numeric <see cref="EventType"/>
      /// values are a contract — do not renumber the enum.
      /// </summary>
      public override string ToString()
      {
         string activity = $"{DateTimeTicks:X}|{ItemId}|{(int)EventType}|{(NeedsReview ? "1" : "")}";

         string[] data = [.. Data.Select(x => x.Replace("|", "\\|", StringComparison.Ordinal))];
         if (data.Length != 0)
         {
            activity += $"|{string.Join("|", data)}";
         }

         return activity;
      }
   }
}
