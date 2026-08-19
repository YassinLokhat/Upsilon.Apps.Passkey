using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   public sealed partial class Database
   {
      private void _save(bool logSaveEvent, bool refreshWarnings = true)
      {
         _saveActivities(rebuildStringActivities: true);
         _saveDatabase(logSaveEvent, refreshWarnings);
      }

      private void _saveDatabase(bool logSaveEvent, bool refreshWarnings = true)
      {
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         Username = User.Username;

         // Record the file's stretching parameters in its (unencrypted) header so
         // the database entry written just below can always be reopened with the
         // exact parameters it was encrypted with.
         FileLocker.Save(_slowHashParameters, HeaderFileEntry);

         // Anchor the activity log's seal inside the (tamper-proof) database so a
         // later rollback or signature strip of the log becomes detectable. The
         // activities were just sealed by the _saveActivities call above (and
         // re-encrypted only when retention pruning dropped entries).
         User.ActivitySealWatermark = ActivityCenter.GetSealedCount();

         // Re-stretching every passkey on each Save is the most expensive step of
         // a save (PBKDF2 × N). Skip it when neither the username nor the master
         // passkeys changed; the session already holds the derived key material.
         if (User.CredentialChanged)
         {
            Passkeys = [CryptographyCenter.GetHash(User.Username), .. User.Passkeys.Select(x => CryptographyCenter.GetSlowHash(x.Reveal(), _slowHashParameters))];
            User.CredentialChanged = false;
         }

         FileLocker.Save(User, DatabaseFileEntry, Passkeys);

         if (logSaveEvent)
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.DatabaseSaved,
               data: [Username],
               needsReview: false);
         }

         AutoSave.Clear(deleteFile: true);

         // DatabaseSaved (and any earlier debounced item events) must hit disk
         // before Save returns, matching the previous durability guarantee.
         ActivityCenter.Flush();

         if (refreshWarnings)
         {
            _ = Task.Run(_lookAtWarningsAsync);
         }

         User.ResetTimer();

         DatabaseSaved?.Invoke(this, EventArgs.Empty);
      }

      private void _saveActivities(bool rebuildStringActivities)
      {
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         ActivityCenter.Username = User.Username;
         ActivityCenter.Save(rebuildStringActivities);
      }

      internal void Close(bool logCloseEvent, bool loginTimeoutReached)
      {
         if (logCloseEvent)
         {
            if (User is not null)
            {
               bool needsReview = AutoSave.Any();

               if (needsReview)
               {
                  // Debounced edits may not have reached the ZIP yet; flush so
                  // the recovery file is present for the next Open.
                  AutoSave.Flush();
               }
               else
               {
                  AutoSave.Clear(deleteFile: true);
               }

               ActivityCenter.AddActivity(itemId: string.Empty,
                  eventType: ActivityEventType.UserLoggedOut,
                  data: [Username, needsReview ? "1" : string.Empty],
                  needsReview);
            }

            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.DatabaseClosed,
               data: [Username],
               needsReview: false);

            // Seal + write while the private key is still available. Must run
            // before User is cleared below.
            ActivityCenter.Flush();
         }
         else
         {
            AutoSave.Clear(deleteFile: false);
            ActivityCenter.CancelPending();
         }

         // Stop the session timer before tearing down the file handle: this both
         // blocks until any in-flight tick finishes and prevents future ticks
         // from operating on the disposed FileLocker.
         User?.StopTimer();

         AutoSave.Dispose();
         ActivityCenter.Dispose();

         User = null;
         Username = string.Empty;
         Passkeys = [];
         Warnings = null;

         FileLocker.Dispose();

         DatabaseClosed?.Invoke(this, new(loginTimeoutReached));
      }

      private void _handleAutoSave(AutoSaveMergeBehavior mergeAutoSave)
      {
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         if (!FileLocker.Exists(AutoSaveFileEntry))
         {
            return;
         }

         switch (mergeAutoSave)
         {
            case AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile:
               AutoSave.ApplyChanges(deleteFile: true);
               // Apply may rename the user; sync before logging so the merge
               // activity carries the post-merge username. Log before _save so
               // the event is sealed with that save and visible to Login's
               // warning scan (skip mid-login refresh below).
               Username = User.Username;
               ActivityCenter.AddActivity(itemId: string.Empty,
                  eventType: _toActivityEventType(mergeAutoSave),
                  data: [Username],
                  needsReview: true);
               _save(logSaveEvent: false, refreshWarnings: false);
               break;
            case AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile:
               AutoSave.ApplyChanges(deleteFile: false);
               Username = User.Username;
               ActivityCenter.AddActivity(itemId: string.Empty,
                  eventType: _toActivityEventType(mergeAutoSave),
                  data: [Username],
                  needsReview: true);
               _saveActivities(rebuildStringActivities: false);
               break;
            case AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile:
               AutoSave.Clear(deleteFile: true);
               ActivityCenter.AddActivity(itemId: string.Empty,
                  eventType: _toActivityEventType(mergeAutoSave),
                  data: [Username],
                  needsReview: true);
               break;
            case AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile:
            default:
               ActivityCenter.AddActivity(itemId: string.Empty,
                  eventType: _toActivityEventType(mergeAutoSave),
                  data: [Username],
                  needsReview: true);
               break;
         }
      }

      // Maps an auto-save handling outcome to the activity event that records it.
      // The two enums are deliberately independent: this explicit switch replaces
      // a brittle numeric cast that relied on their values coinciding, so
      // reordering either enum can no longer silently log the wrong event. A new
      // AutoSaveMergeBehavior value now forces a compile-time review here.
      private static ActivityEventType _toActivityEventType(AutoSaveMergeBehavior mergeBehavior) => mergeBehavior switch
      {
         AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile => ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile,
         AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile => ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile,
         AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile => ActivityEventType.DontMergeAndRemoveAutoSaveFile,
         AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile => ActivityEventType.DontMergeAndKeepAutoSaveFile,
         _ => ActivityEventType.None,
      };
   }
}
