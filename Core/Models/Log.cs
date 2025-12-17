using System.Security.AccessControl;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal class Log : ILog
   {
      #region ILog interface

      public DateTime DateTime => new(DateTimeTicks);

      public string Source { get; set; } = string.Empty;

      public string Target { get; set; } = string.Empty;

      public string Data { get; set; } = string.Empty;

      public LogEventType EventType { get; set; } = LogEventType.None;

      public bool NeedsReview { get; set; } = true;

      public string Message => _buildMessage();

      #endregion

      public long DateTimeTicks { get; set; }

      public Log() { }

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
            Source = info[1].Trim();
         }

         if (info.Length > 2)
         {
            Target = info[2].Trim();
         }

         if (info.Length > 3)
         {
            Target = info[3].Trim();
         }

         if (info.Length > 4
            && byte.TryParse(info[4], out byte eventType))
         {
            EventType = (LogEventType)eventType;
         }

         if (info.Length > 5)
         {
            NeedsReview = !string.IsNullOrEmpty(info[5]);
         }
      }

      public override string ToString()
      {
         return $"{DateTimeTicks}|{Source}|{Target}|{Data}|{((int)EventType)}|{(NeedsReview ? "1" : "")}";
      }

      private string _buildMessage()
      {
         /*
         string logMessage = action switch
            {
               Change.Type.Add => $"{itemName} has been added to {containerName}",
               Change.Type.Delete => $"{itemName} has been removed from {containerName}",
               _ => $"{itemName}'s {fieldName.ToSentenceCase().ToLower()} has been {(string.IsNullOrWhiteSpace(readableValue) ? $"updated" : $"set to {readableValue}")}",
            };
         Database.Logs.AddLog($"User {Username}'s login session timeout reached", needsReview: true);
         Logs.AddLog($"User {Username} login failed at level {passwordException.PasswordLevel}", needsReview: true);
         Logs.AddLog($"User {Username} logged in", needsReview: false);
         Logs.AddLog($"Importing data from file : '{filePath}'", needsReview: true);
         Logs.AddLog($"Import completed successfully", needsReview: true);
         Logs.AddLog($"Import failed because {errorLog}", needsReview: true);
         Logs.AddLog($"Exporting data to file : '{filePath}'", needsReview: true);
         Logs.AddLog($"Export completed successfully", needsReview: true);
         Logs.AddLog($"Export failed because {errorLog}", needsReview: true);
         database.Logs.AddLog($"User {username}'s database created", needsReview: false);
         database.Logs.AddLog($"User {username}'s database opened", needsReview: false);
         Logs.AddLog($"User {Username}'s database saved", needsReview: false);
         string logoutLog = $"User {Username} logged out";
         bool needsReview = AutoSave.Any();
         if (needsReview)
            {
               logoutLog += " without saving";
               }
               else
               {
                  AutoSave.Clear(deleteFile: true);
               }
               Logs.AddLog(logoutLog, needsReview);
            }
         Logs.AddLog($"User {Username}'s database closed", needsReview: false);

         case AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile:
               AutoSave.ApplyChanges(deleteFile: true);
               Logs.AddLog($"User {Username}'s autosave merged and saved", needsReview: true);
               _save(logSaveEvent: false);
               break;
            case AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile:
               AutoSave.ApplyChanges(deleteFile: false);
               Logs.AddLog($"User {Username}'s autosave merged without saving", needsReview: true);
               _saveLogs();
               break;
            case AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile:
               AutoSave.Clear(deleteFile: true);
               Logs.AddLog($"User {Username}'s autosave not merged and removed", needsReview: true);
               break;
            case AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile:
            default:
               Logs.AddLog($"User {Username}'s autosave not merged and keeped.", needsReview: true);
               break;

         */
         return ToString();
      }
   }
}
