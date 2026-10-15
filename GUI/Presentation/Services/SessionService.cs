using Upsilon.Apps.Passkey.GUI.Presentation.Alerts;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   internal sealed class SessionService : ISessionService, IDisposable
   {
      private readonly Lock _gate = new();
      private readonly Lock _writeBackGate = new();
      private Task _writeBackTail = Task.CompletedTask;
      private readonly Func<bool> _isOfflineFilterBusy;
      private readonly Dictionary<IDatabase, IVaultSource> _sources = new(ReferenceEqualityComparer.Instance);
      private readonly Dictionary<IDatabase, TaskCompletionSource<bool>> _deleting = new(ReferenceEqualityComparer.Instance);
      private readonly Dictionary<IDatabase, (string FileName, TaskCompletionSource<VaultRenameOutcome> Done)> _renaming = new(ReferenceEqualityComparer.Instance);
      private IDatabase? _database;
      private IClipboardService? _clipboard;
      private bool _disposed;

      /// <param name="isOfflineFilterBusy">
      /// Host hook: an offline <c>.pkbf</c> build is running, so a missing filter
      /// is not reported as a posture issue yet.
      /// </param>
      public SessionService(Func<bool>? isOfflineFilterBusy = null)
      {
         _isOfflineFilterBusy = isOfflineFilterBusy ?? (static () => false);
         Alerts = new AlertBroker(_evaluateHostPosture);
      }

      public IDatabase? Database
      {
         get
         {
            lock (_gate)
            {
               return _database;
            }
         }
      }

      public IUser? User => Database?.User;

      public AlertBroker Alerts { get; }

      public event EventHandler? SessionChanged;

      public event EventHandler<VaultWriteBackEventArgs>? WriteBackCompleted;

      public void TrackVaultSource(IDatabase database, IVaultSource source)
      {
         ArgumentNullException.ThrowIfNull(database);
         ArgumentNullException.ThrowIfNull(source);

         lock (_gate)
         {
            _sources[database] = source;
         }

         // Core raises DatabaseClosed after the final flush and once the file
         // lock is released, so the working copy is complete at that point.
         void onClosed(object? sender, LogoutEventArgs e)
         {
            database.DatabaseClosed -= onClosed;
            TaskCompletionSource<bool>? deleted;
            (string FileName, TaskCompletionSource<VaultRenameOutcome> Done) rename;
            bool renaming;
            lock (_gate)
            {
               _ = _sources.Remove(database);
               _ = _deleting.Remove(database, out deleted);
               renaming = _renaming.Remove(database, out rename);
            }

            _ = _enqueue(deleted is not null
               ? () => _deleteThenDisposeAsync(source, deleted)
               : renaming
               ? () => _writeBackRenameThenDisposeAsync(source, rename.FileName, rename.Done)
               : () => _writeBackThenDisposeAsync(source));
         }

         database.DatabaseClosed += onClosed;
      }

      public async Task<VaultWriteBackResult?> FlushToSourceAsync()
      {
         IDatabase? database;
         IVaultSource? source = null;
         lock (_gate)
         {
            database = _database;
            if (database is not null)
            {
               _ = _sources.TryGetValue(database, out source);
            }
         }

         if (database is null || source is null)
         {
            return null;
         }

         VaultWriteBackResult? result = null;
         await _enqueue(async () =>
         {
            try
            {
               database.FlushPendingChanges();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
               // Writing back the older file would claim the latest edits are saved.
               AppServices.Log.Warn($"Flushing pending changes failed: {ErrorText.ForLog(ex)}");
               result = VaultWriteBackResult.Failed;
               _report(VaultWriteBackResult.Failed, ex);
               return;
            }

            (VaultWriteBackResult written, Exception? failure) = await _tryWriteBackAsync(source).ConfigureAwait(false);
            result = written;
            _report(written, failure);
         }).ConfigureAwait(false);

         return result;
      }

      public void DeleteVault() => _ = DeleteVaultAsync();

      public Task<bool> DeleteVaultAsync()
      {
         IDatabase database;
         TaskCompletionSource<bool> done = new(TaskCreationOptions.RunContinuationsAsynchronously);
         bool tracked;
         lock (_gate)
         {
            database = _database ?? throw new InvalidOperationException("No active session.");
            tracked = _sources.ContainsKey(database);
            if (tracked)
            {
               _deleting[database] = done;
            }
         }

         try
         {
            database.Delete();
         }
         catch
         {
            lock (_gate)
            {
               _ = _deleting.Remove(database);
            }

            throw;
         }

         EndSession(closeDatabase: false);

         if (!tracked)
         {
            // Untracked vaults are the real file: Core's delete is the whole job.
            done.TrySetResult(true);
         }

         return done.Task;
      }

      public async Task<VaultRenameOutcome> EndSessionAndRenameAsync(string newFileName)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(newFileName);

         IDatabase database;
         TaskCompletionSource<VaultRenameOutcome> done = new(TaskCreationOptions.RunContinuationsAsynchronously);
         bool tracked;
         lock (_gate)
         {
            database = _database ?? throw new InvalidOperationException("No active session.");
            tracked = _sources.ContainsKey(database);
            if (tracked)
            {
               _renaming[database] = (newFileName, done);
            }
         }

         string databaseFile = database.DatabaseFile;
         EndSession();

         if (tracked)
         {
            return await done.Task.ConfigureAwait(false);
         }

         if (AppServices.VaultSources is not { } factory)
         {
            return new VaultRenameOutcome(false, VaultWriteBackResult.Skipped, databaseFile);
         }

         IVaultSource source = await factory.OpenPathAsync(databaseFile).ConfigureAwait(false);
         _ = _enqueue(() => _writeBackRenameThenDisposeAsync(source, newFileName, done));
         return await done.Task.ConfigureAwait(false);
      }

      public void StartSession(IDatabase database)
      {
         ArgumentNullException.ThrowIfNull(database);

         EndSession();

         lock (_gate)
         {
            _database = database;
         }

         _attachClipboard();
         Alerts.Attach(database);

         AppServices.Log.Info("Session started.");
         UiThread.Post(() =>
         {
            _applySessionLanguage();
            _applySessionTheme();
            SessionChanged?.Invoke(this, EventArgs.Empty);
         });
      }

      public void EndSession(bool closeDatabase = true)
      {
         IDatabase? database;
         lock (_gate)
         {
            database = _database;
            _database = null;
         }

         _detachClipboard();
         AppServices.Clipboard?.ClearIfStillOwned();

         if (database is null)
         {
            return;
         }

         try
         {
            Alerts.Detach();

            if (closeDatabase)
            {
               database.Close();
            }
         }
         finally
         {
            AppServices.Log.Info("Session ended.");
            UiThread.Post(() =>
            {
               AppSettings? settings = AppServices.Settings?.Current;
               _ = LocalizationService.Apply(settings?.Language);
               AppServices.Theme?.Apply(ThemePreferences.ResolveEffective(settings?.Theme, null));
               SessionChanged?.Invoke(this, EventArgs.Empty);
            });
         }
      }

      public async Task<bool> FlushWriteBacksAsync(TimeSpan timeout)
      {
         Task tail;
         lock (_writeBackGate)
         {
            tail = _writeBackTail;
         }

         try
         {
            await tail.WaitAsync(timeout).ConfigureAwait(false);
            return true;
         }
         catch (TimeoutException)
         {
            AppServices.Log.Warn("Vault write-back still pending after the flush timeout.");
            return false;
         }
      }
      public void ApplySessionLanguage()
         => _applySessionLanguage();

      public void ApplySessionTheme()
         => _applySessionTheme();

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         EndSession();
         Alerts.Dispose();
         SessionChanged = null;
         WriteBackCompleted = null;
      }

      private void _applySessionLanguage()
      {
         _ = LocalizationService.ApplyEffective(
            AppServices.Settings?.Current.Language,
            User?.Settings.Language);
      }

      private void _applySessionTheme()
      {
         AppServices.Theme?.Apply(ThemePreferences.ResolveEffective(
            AppServices.Settings?.Current.Theme,
            User?.Settings.Theme));
      }

      private void _attachClipboard()
      {
         IClipboardService? clipboard = AppServices.Clipboard;
         lock (_gate)
         {
            if (ReferenceEquals(_clipboard, clipboard))
            {
               return;
            }

            if (_clipboard is not null)
            {
               _clipboard.SecretCopied -= _onSecretCopied;
            }

            _clipboard = clipboard;
            if (clipboard is not null)
            {
               clipboard.SecretCopied += _onSecretCopied;
            }
         }
      }

      private void _detachClipboard()
      {
         lock (_gate)
         {
            if (_clipboard is not null)
            {
               _clipboard.SecretCopied -= _onSecretCopied;
               _clipboard = null;
            }
         }
      }

      private void _onSecretCopied(object? sender, ClipboardSecretCopiedEventArgs e)
         => User?.RememberClipboardSecret(e.Text);

      /// <summary>
      /// One source operation at a time, in request order: a new session or a
      /// flush must not race a previous write on the same durable source.
      /// </summary>
      private Task _enqueue(Func<Task> work)
      {
         lock (_writeBackGate)
         {
            _writeBackTail = _runAfterAsync(_writeBackTail, work);
            return _writeBackTail;
         }
      }

      private static async Task _runAfterAsync(Task previous, Func<Task> work)
      {
         // A faulted predecessor must not block every later write-back.
         await previous.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
         await work().ConfigureAwait(false);
      }

      private async Task _writeBackThenDisposeAsync(IVaultSource source)
      {
         (VaultWriteBackResult result, Exception? failure) = await _tryWriteBackAsync(source).ConfigureAwait(false);
         await _disposeAsync(source).ConfigureAwait(false);
         _report(result, failure);
      }

      private async Task _writeBackRenameThenDisposeAsync(
         IVaultSource source,
         string newFileName,
         TaskCompletionSource<VaultRenameOutcome> done)
      {
         (VaultWriteBackResult result, Exception? failure) = await _tryWriteBackAsync(source).ConfigureAwait(false);
         bool renamed = false;

         if (result is VaultWriteBackResult.Written or VaultWriteBackResult.Skipped)
         {
            try
            {
               renamed = await source.RenameAsync(newFileName).ConfigureAwait(false);
            }
            catch (Exception ex) when (_isSourceFailure(ex))
            {
               AppServices.Log.Error($"Vault rename failed: {ErrorText.ForLog(ex)}");
            }
         }

         string location = source.Location;
         await _disposeAsync(source).ConfigureAwait(false);
         _report(result, failure);
         done.TrySetResult(new VaultRenameOutcome(renamed, result, location));
      }

      private async Task _deleteThenDisposeAsync(IVaultSource source, TaskCompletionSource<bool> done)
      {
         bool deleted;
         try
         {
            deleted = await source.DeleteAsync().ConfigureAwait(false);
         }
         catch (Exception ex) when (_isSourceFailure(ex))
         {
            AppServices.Log.Error($"Vault delete failed: {ErrorText.ForLog(ex)}");
            deleted = false;
         }

         await _disposeAsync(source).ConfigureAwait(false);
         if (!deleted)
         {
            UiThread.Post(() => WriteBackCompleted?.Invoke(
               this,
               new VaultWriteBackEventArgs(VaultWriteBackResult.Failed, Strings.Login_VaultDeleteRefused)));
         }

         done.TrySetResult(deleted);
      }

      private static async Task<(VaultWriteBackResult Result, Exception? Failure)> _tryWriteBackAsync(IVaultSource source)
      {
         try
         {
            return (await source.WriteBackAsync().ConfigureAwait(false), null);
         }
         catch (Exception ex) when (_isSourceFailure(ex))
         {
            AppServices.Log.Error($"Vault write-back failed: {ErrorText.ForLog(ex)}");
            return (VaultWriteBackResult.Failed, ex);
         }
      }

      private static async Task _disposeAsync(IVaultSource source)
      {
         try
         {
            await source.DisposeAsync().ConfigureAwait(false);
         }
         catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
         {
            AppServices.Log.Warn($"Vault working copy cleanup failed: {ErrorText.ForLog(ex)}");
         }
      }

      private static bool _isSourceFailure(Exception ex)
         => ex is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or NotSupportedException;

      private void _report(VaultWriteBackResult result, Exception? failure)
      {
         if (result is not (VaultWriteBackResult.Conflict or VaultWriteBackResult.Failed or VaultWriteBackResult.SourceDamaged))
         {
            return;
         }

         UiThread.Post(() =>
         {
            string message = result switch
            {
               VaultWriteBackResult.Conflict => Strings.Login_WriteBackConflict,
               VaultWriteBackResult.SourceDamaged => Strings.Login_WriteBackSourceDamaged,
               _ when failure is null => Strings.Login_WriteBackFailed,
               _ => Strings.Format(nameof(Strings.Login_WriteBackFailedWithReason), ErrorText.Describe(failure)),
            };
            WriteBackCompleted?.Invoke(this, new VaultWriteBackEventArgs(result, message));
         });
      }
      private HostSecurityIssue _evaluateHostPosture()
      {
         HostSecurityIssue issues = HostSecurityIssue.None;

         if ((AppServices.Settings?.Current.LoginIdleTimeoutSeconds ?? 1) <= 0)
         {
            issues |= HostSecurityIssue.IdleLoginDisabled;
         }

         if (AppServices.Capabilities.SupportsOfflineLeakFilter
            && !AppServices.PasswordFactory.HasLocalFilter
            && !_isOfflineFilterBusy())
         {
            issues |= HostSecurityIssue.OfflineLeakFilterUnavailable;
         }

         return issues;
      }
   }
}
