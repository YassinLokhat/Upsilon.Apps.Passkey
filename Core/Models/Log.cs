using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal class Log : ILog
   {
      #region ILog interface

      public DateTime DateTime => new(DateTimeTicks);

      public string ItemId { get; } = string.Empty;

      public LogEventType EventType { get; set; } = LogEventType.None;

      public bool NeedsReview { get; set; } = true;

      public string Message => _buildMessage();

      #endregion

      public long DateTimeTicks { get; set; }
      public string[] Data { get; set; } = [];

      public Log(long dateTimeTicks, string itemId, LogEventType eventType, string[] data, bool needsReview)
      {
         DateTimeTicks = dateTimeTicks;
         ItemId = itemId;
         EventType = eventType;
         Data = data;
         NeedsReview = needsReview;
      }

      public Log(string log)
      {
         string[] info = log.Split('|');

         if (info.Length > 0
            && long.TryParse(info[0], out long ticks))
         {
            DateTimeTicks = ticks;
         }

         if (info.Length > 1)
         {
            ItemId = info[1];
         }

         if (info.Length > 2
            && byte.TryParse(info[2], out byte eventType))
         {
            EventType = (LogEventType)eventType;
         }

         if (info.Length > 3)
         {
            NeedsReview = !string.IsNullOrEmpty(info[3]);
         }

         if (info.Length > 4)
         {
            log = string.Join("|", info[4..]);
            log = log.Replace("|", "/|");
            log = log.Replace("\\/|", "\\|");
            info = log.Split("/|");
            Data = [.. info.Select(x => x.Replace("\\|", "|"))];
         }
      }

      public override string ToString()
      {
         string log = $"{DateTimeTicks}|{ItemId}|{(int)EventType}|{(NeedsReview ? "1" : "")}";

         string[] data = [.. Data.Select(x => x.Replace("|", "\\|"))];
         if (data.Length != 0)
         {
            log += $"|{string.Join("|", data)}";
         }

         return log;
      }

      private string _buildMessage()
      {
         string message = EventType switch
         {
            LogEventType.MergeAndSaveThenRemoveAutoSaveFile => $"User {Data[0]}'s autosave merged and saved",
            LogEventType.MergeWithoutSavingAndKeepAutoSaveFile => $"User {Data[0]}'s autosave merged without saving",
            LogEventType.DontMergeAndRemoveAutoSaveFile => $"User {Data[0]}'s autosave not merged and removed",
            LogEventType.DontMergeAndKeepAutoSaveFile => $"User {Data[0]}'s autosave not merged and keeped",
            LogEventType.DatabaseCreated => $"User {Data[0]}'s database created",
            LogEventType.DatabaseOpened => $"User {Data[0]}'s database opened",
            LogEventType.DatabaseSaved => $"User {Data[0]}'s database saved",
            LogEventType.DatabaseClosed => $"User {Data[0]}'s database closed",
            LogEventType.LoginSessionTimeoutReached => $"User {Data[0]}'s login session timeout reached",
            LogEventType.LoginFailed => $"User {Data[0]} login failed at level {Data[1]}",
            LogEventType.UserLoggedIn => $"User {Data[0]} logged in",
            LogEventType.UserLoggedOut => $"User {Data[0]} logged out {(!string.IsNullOrEmpty(Data[1]) ? "without saving" : "")}",
            LogEventType.ImportingDataStarted => $"Importing data from file : '{Data[0]}'",
            LogEventType.ImportingDataSucceded => $"Import completed successfully",
            LogEventType.ImportingDataFailed => $"Import failed because {Data[0]}",
            LogEventType.ExportingDataStarted => $"Exporting data to file : '{Data[0]}'",
            LogEventType.ExportingDataSucceded => $"Export completed successfully",
            LogEventType.ExportingDataFailed => $"Export failed because {Data[0]}",
            LogEventType.ItemUpdated => $"{Data[0]}'s {Data[1].ToSentenceCase().ToLower()} has been {(string.IsNullOrWhiteSpace(Data[2]) ? $"updated" : $"set to {Data[2]}")}",
            LogEventType.ItemAdded => $"{Data[2]} has been added to {Data[0]}",
            LogEventType.ItemDeleted => $"{Data[2]} has been removed from {Data[0]}",
            _ => ToString(),
         };

         return message.Trim();
      }
   }
}
