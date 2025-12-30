using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal class Activity : IActivity
   {
      #region IActivity interface

      public DateTime DateTime => new(DateTimeTicks);

      public string ItemId { get; } = string.Empty;

      public ActivityEventType EventType { get; set; } = ActivityEventType.None;

      public bool NeedsReview { get; set; } = true;

      public string Message => _buildMessage();

      #endregion

      public long DateTimeTicks { get; set; }
      public string[] Data { get; set; } = [];

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
               .Replace("|", "/|")
               .Replace("\\/|", "\\|");
            info = activity.Split("/|");
            Data = [.. info.Select(x => x.Replace("\\|", "|"))];
         }
      }

      public override string ToString()
      {
         string activity = $"{DateTimeTicks:X}|{ItemId}|{(int)EventType}|{(NeedsReview ? "1" : "")}";

         string[] data = [.. Data.Select(x => x.Replace("|", "\\|"))];
         if (data.Length != 0)
         {
            activity += $"|{string.Join("|", data)}";
         }

         return activity;
      }

      private string _buildMessage()
      {
         string message = EventType switch
         {
            ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile => $"User {Data[0]}'s autosave merged and saved",
            ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile => $"User {Data[0]}'s autosave merged without saving",
            ActivityEventType.DontMergeAndRemoveAutoSaveFile => $"User {Data[0]}'s autosave not merged and removed",
            ActivityEventType.DontMergeAndKeepAutoSaveFile => $"User {Data[0]}'s autosave not merged and keeped",
            ActivityEventType.DatabaseCreated => $"User {Data[0]}'s database created",
            ActivityEventType.DatabaseOpened => $"User {Data[0]}'s database opened",
            ActivityEventType.DatabaseSaved => $"User {Data[0]}'s database saved",
            ActivityEventType.DatabaseClosed => $"User {Data[0]}'s database closed",
            ActivityEventType.LoginSessionTimeoutReached => $"User {Data[0]}'s login session timeout reached",
            ActivityEventType.LoginFailed => $"User {Data[0]} login failed at level {Data[1]}",
            ActivityEventType.UserLoggedIn => $"User {Data[0]} logged in",
            ActivityEventType.UserLoggedOut => $"User {Data[0]} logged out {(!string.IsNullOrEmpty(Data[1]) ? "without saving" : "")}",
            ActivityEventType.ImportingDataStarted => $"Importing data from file : '{Data[0]}'",
            ActivityEventType.ImportingDataSucceded => $"Import completed successfully",
            ActivityEventType.ImportingDataFailed => $"Import failed because {Data[0]}",
            ActivityEventType.ExportingDataStarted => $"Exporting data to file : '{Data[0]}'",
            ActivityEventType.ExportingDataSucceded => $"Export completed successfully",
            ActivityEventType.ExportingDataFailed => $"Export failed because {Data[0]}",
            ActivityEventType.ItemUpdated => $"{Data[0]}'s {Data[1].ToSentenceCase().ToLower()} has been {(string.IsNullOrWhiteSpace(Data[2]) ? $"updated" : $"set to {Data[2]}")}",
            ActivityEventType.ItemAdded => $"{Data[2]} has been added to {Data[0]}",
            ActivityEventType.ItemDeleted => $"{Data[2]} has been removed from {Data[0]}",
            _ => ToString(),
         };

         return message.Trim();
      }
   }
}
