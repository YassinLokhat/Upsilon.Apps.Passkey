using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Warnings
{
   /// <summary>
   /// Aggregates Core per-kind warning events and host posture warnings, then
   /// applies the user's <see cref="ISettings.WarningsToNotify"/> filter.
   /// </summary>
   internal sealed class WarningBroker : IDisposable
   {
      private readonly object _gate = new();
      private readonly Dictionary<string, IReadOnlyList<IWarning>> _byKind = new(StringComparer.Ordinal);
      private IDatabase? _database;
      private bool _disposed;

      public event EventHandler? NotifiedWarningsChanged;

      public IReadOnlyList<string> AvailableKinds { get; } =
      [
         .. WarningKinds.AllCore,
         .. WarningKinds.AllHost,
      ];

      public IReadOnlyList<IWarning> GetNotifiedWarnings()
      {
         lock (_gate)
         {
            WarningKindList? mask = _database?.User?.Settings.WarningsToNotify;
            if (mask is null || mask.Count == 0)
            {
               return [];
            }

            List<IWarning> result = [];
            foreach (KeyValuePair<string, IReadOnlyList<IWarning>> pair in _byKind)
            {
               if (!mask.Contains(pair.Key))
               {
                  continue;
               }

               result.AddRange(pair.Value);
            }

            return result;
         }
      }

      public IReadOnlyList<IWarning> GetNotifiedWarnings(string kind)
         => [.. GetNotifiedWarnings().Where(w => w.Kind == kind)];

      public IReadOnlyList<IWarning> GetAllWarnings(string kind)
      {
         lock (_gate)
         {
            return _byKind.TryGetValue(kind, out IReadOnlyList<IWarning>? list) ? list : [];
         }
      }

      public static Brush BrushFor(WarningSeverity severity)
         => severity >= WarningSeverity.Critical
            ? SemanticBrushes.Danger
            : severity >= WarningSeverity.Warning
               ? SemanticBrushes.Warning
               : SemanticBrushes.Info;

      public static Brush BrushFor(IEnumerable<IWarning> warnings)
      {
         WarningSeverity max = WarningSeverity.Info;
         foreach (IWarning warning in warnings)
         {
            if (warning.Severity > max)
            {
               max = warning.Severity;
            }
         }

         return BrushFor(max);
      }

      public void Attach(IDatabase database)
      {
         ArgumentNullException.ThrowIfNull(database);
         Detach();

         _database = database;
         _subscribe(database);

         RefreshHostWarnings();

         foreach (KeyValuePair<string, IReadOnlyList<IWarning>> pair in database.CoreWarnings)
         {
            _store(pair.Key, pair.Value);
         }

         _raiseChanged();

         if (database.User is not null)
         {
            database.RefreshWarnings();
         }
      }

      public void Detach()
      {
         if (_database is not null)
         {
            _unsubscribe(_database);
            _database = null;
         }

         lock (_gate)
         {
            _byKind.Clear();
         }
      }

      public void RefreshHostWarnings()
      {
         HostSecurityIssue issues = HostSecurityIssue.None;

         if (AppInfo.AppSettings.LoginIdleTimeoutSeconds <= 0)
         {
            issues |= HostSecurityIssue.IdleLoginDisabled;
         }

         if (!AppServices.PasswordFactory.HasLocalFilter)
         {
            issues |= HostSecurityIssue.OfflineLeakFilterUnavailable;
         }

         IReadOnlyList<IWarning> warnings = issues == HostSecurityIssue.None
            ? []
            : [new HostSecuritySettingsWarning(issues)];

         _store(WarningKinds.HostSecuritySettings, warnings);
         _raiseChanged();
      }

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         Detach();
      }

      private void _subscribe(IDatabase database)
      {
         database.ActivityReviewWarningsChanged += _onKindChanged;
         database.PasswordUpdateReminderWarningsChanged += _onKindChanged;
         database.DuplicatedPasswordsWarningsChanged += _onKindChanged;
         database.PasswordLeakedWarningsChanged += _onKindChanged;
         database.VaultSecuritySettingsWarningsChanged += _onKindChanged;
         database.InsufficientPasskeysWarningsChanged += _onKindChanged;
         database.WeakPasskeyWarningsChanged += _onKindChanged;
         database.PasskeyLeakedWarningsChanged += _onKindChanged;
         database.WeakAccountPasswordWarningsChanged += _onKindChanged;
         database.PasskeyReuseWarningsChanged += _onKindChanged;
         database.CoreWarningsScanCompleted += _onScanCompleted;
      }

      private void _unsubscribe(IDatabase database)
      {
         database.ActivityReviewWarningsChanged -= _onKindChanged;
         database.PasswordUpdateReminderWarningsChanged -= _onKindChanged;
         database.DuplicatedPasswordsWarningsChanged -= _onKindChanged;
         database.PasswordLeakedWarningsChanged -= _onKindChanged;
         database.VaultSecuritySettingsWarningsChanged -= _onKindChanged;
         database.InsufficientPasskeysWarningsChanged -= _onKindChanged;
         database.WeakPasskeyWarningsChanged -= _onKindChanged;
         database.PasskeyLeakedWarningsChanged -= _onKindChanged;
         database.WeakAccountPasswordWarningsChanged -= _onKindChanged;
         database.PasskeyReuseWarningsChanged -= _onKindChanged;
         database.CoreWarningsScanCompleted -= _onScanCompleted;
      }

      private void _onKindChanged(object? sender, WarningsChangedEventArgs e)
      {
         _store(e.Kind, e.Warnings);
      }

      private void _onScanCompleted(object? sender, EventArgs e)
      {
         // Host posture can change while a scan runs (filter attach/detach).
         RefreshHostWarnings();
         _raiseChanged();
      }

      private void _store(string kind, IReadOnlyList<IWarning> warnings)
      {
         lock (_gate)
         {
            _byKind[kind] = warnings;
         }
      }

      private void _raiseChanged()
         => NotifiedWarningsChanged?.Invoke(this, EventArgs.Empty);
   }
}
