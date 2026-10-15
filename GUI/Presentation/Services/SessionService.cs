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

         // Core raises DatabaseClosed after the final flush and once the file
         // lock is released, so the working copy is complete at that point.
         void onClosed(object? sender, LogoutEventArgs e)
         {
            database.DatabaseClosed -= onClosed;
            _queueWriteBack(source);
         }

         database.DatabaseClosed += onClosed;
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

      private void _queueWriteBack(IVaultSource source)
      {
         // One write-back at a time, in close order: a new session must not
         // race the previous one on the same durable source.
         lock (_writeBackGate)
         {
            _writeBackTail = _writeBackAfterAsync(_writeBackTail, source);
         }
      }

      private async Task _writeBackAfterAsync(Task previous, IVaultSource source)
      {
         await previous.ConfigureAwait(false);

         VaultWriteBackResult result;
         Exception? failure = null;
         try
         {
            result = await source.WriteBackAsync().ConfigureAwait(false);
         }
         catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or NotSupportedException)
         {
            AppServices.Log.Error($"Vault write-back failed: {ErrorText.ForLog(ex)}");
            result = VaultWriteBackResult.Failed;
            failure = ex;
         }

         try
         {
            await source.DisposeAsync().ConfigureAwait(false);
         }
         catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
         {
            AppServices.Log.Warn($"Vault working copy cleanup failed: {ErrorText.ForLog(ex)}");
         }

         if (result is VaultWriteBackResult.Conflict or VaultWriteBackResult.Failed)
         {
            UiThread.Post(() =>
            {
               string message = result == VaultWriteBackResult.Conflict
                  ? Strings.Login_WriteBackConflict
                  : failure is null
                     ? Strings.Login_WriteBackFailed
                     : Strings.Format(nameof(Strings.Login_WriteBackFailedWithReason), ErrorText.Describe(failure));
               WriteBackCompleted?.Invoke(this, new VaultWriteBackEventArgs(result, message));
            });
         }
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
