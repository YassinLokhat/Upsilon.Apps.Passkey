using System.Text.RegularExpressions;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// One audit-log row. Ciphertext is RSA-hybrid; plaintext lives only after login.
   /// </summary>
   internal sealed partial class Activity : IActivity
   {
      #region IActivity interface

      public DateTime DateTime => new(DateTimeTicks);

      public string ItemId { get; } = string.Empty;

      public string? Username { get; set; }

      public string? ServiceName { get; set; }

      public string? AccountName { get; set; }

      public string? FieldName { get; set; }

      public string? FieldValue { get; set; }

      public string? ParentName { get; set; }

      public ActivityEventType EventType { get; set; } = ActivityEventType.None;

      public bool NeedsReview { get; set; } = true;

      #endregion

      public long DateTimeTicks { get; set; }

      public Activity(long dateTimeTicks,
         string itemId,
         string? username,
         string? serviceName,
         string? accountName,
         string? fieldName,
         string? fieldValue,
         string? parentName,
         ActivityEventType eventType, bool needsReview)
      {
         DateTimeTicks = dateTimeTicks;
         ItemId = itemId;
         Username = username;
         ServiceName = serviceName;
         AccountName = accountName;
         FieldName = fieldName;
         FieldValue = fieldValue;
         ParentName = parentName;
         EventType = eventType;
         NeedsReview = needsReview;
      }

      public Activity(string activity)
      {
         string[] info = _splitUnescapePipes(activity);
         int index = 0;

         if (info.Length > index)
         {
            DateTimeTicks = Convert.ToInt64(info[index], 16);
         }

         index++;
         if (info.Length > index)
         {
            ItemId = info[index];
         }

         index++;
         if (info.Length > index)
         {
            Username = info[index];
         }

         index++;
         if (info.Length > index)
         {
            ServiceName = info[index];
         }

         index++;
         if (info.Length > index)
         {
            AccountName = info[index];
         }

         index++;
         if (info.Length > index
            && byte.TryParse(info[index], out byte eventType))
         {
            EventType = (ActivityEventType)eventType;
         }

         index++;
         if (info.Length > index)
         {
            NeedsReview = !string.IsNullOrEmpty(info[index]);
         }

         index++;
         if (info.Length > index)
         {
            ParentName = !string.IsNullOrEmpty(info[index]) ? info[index] : null;
         }

         index++;
         if (info.Length > index)
         {
            FieldName = !string.IsNullOrEmpty(info[index]) ? info[index] : null;
         }

         index++;
         if (info.Length > index)
         {
            info = info[index..];
            FieldValue = string.Join('|', info);

            if (string.IsNullOrEmpty(FieldValue))
            {
               FieldValue = null;
            }
         }
      }

      /// <summary>
      /// Persistence wire format: ticks|itemId|username|serviceName|accountName|fieldName|fieldValue|parentName|eventType|needsReview with
      /// <c>|</c> escaped as <c>\|</c> inside data. Numeric <see cref="EventType"/>
      /// values are a contract — do not renumber the enum.
      /// </summary>
      public override string ToString()
         => $"{DateTimeTicks:X}" +
            $"|{ItemId}" +
            $"|{_escapePipes(Username)}" +
            $"|{_escapePipes(ServiceName)}" +
            $"|{_escapePipes(AccountName)}" +
            $"|{(int)EventType}" +
            $"|{(NeedsReview ? "1" : "")}" +
            $"|{_escapePipes(ParentName)}" +
            $"|{FieldName}" +
            $"|{_escapePipes(FieldValue)}";

      private static string[] _splitUnescapePipes(string source)
         => SplitUnescapePipes().Split(source);

      private static string? _escapePipes(string? source)
         => source?.Replace("|", "\\|", StringComparison.InvariantCulture);

      [GeneratedRegex(@"(?<!\\)\|")]
      private static partial Regex SplitUnescapePipes();
   }
}
