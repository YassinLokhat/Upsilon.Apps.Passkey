using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Alerts
{
   /// <summary>
   /// Aggregates Core per-kind alert events and host posture alerts, then
   /// applies the user's <see cref="ISettings.AlertsToNotify"/> filter.
   /// </summary>
   internal sealed class AlertBroker : IDisposable
   {
      private readonly object _gate = new();
      private readonly Dictionary<string, IReadOnlyList<IAlert>> _byKind = new(StringComparer.Ordinal);
      private IDatabase? _database;
      private bool _disposed;

      public event EventHandler? NotifiedAlertsChanged;

      public IReadOnlyList<string> AvailableKinds { get; } =
      [
         .. AlertKinds.AllCore,
         .. AlertKinds.AllHost,
      ];

      public IReadOnlyList<IAlert> GetNotifiedAlerts()
      {
         lock (_gate)
         {
            AlertKindList? mask = _database?.User?.Settings.AlertsToNotify;
            if (mask is null || mask.Count == 0)
            {
               return [];
            }

            List<IAlert> result = [];
            foreach (KeyValuePair<string, IReadOnlyList<IAlert>> pair in _byKind)
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

      public IReadOnlyList<IAlert> GetNotifiedAlerts(string kind)
         => [.. GetNotifiedAlerts().Where(w => w.Kind == kind)];

      public IReadOnlyList<IAlert> GetAllAlerts(string kind)
      {
         lock (_gate)
         {
            return _byKind.TryGetValue(kind, out IReadOnlyList<IAlert>? list) ? list : [];
         }
      }

      public static Brush BrushFor(AlertSeverity severity)
         => severity switch
         {
            AlertSeverity.Critical => SemanticBrushes.Danger,
            AlertSeverity.Warning => SemanticBrushes.Warning,
            _ => SemanticBrushes.Info,
         };

      public static AlertSeverity MaxSeverity(IEnumerable<IAlert> alerts)
      {
         AlertSeverity max = AlertSeverity.Info;
         foreach (IAlert alert in alerts)
         {
            if (alert.Severity > max)
            {
               max = alert.Severity;
            }
         }

         return max;
      }

      public static Brush BrushFor(IEnumerable<IAlert> alerts)
         => BrushFor(MaxSeverity(alerts));

      public void Attach(IDatabase database)
      {
         ArgumentNullException.ThrowIfNull(database);
         Detach();

         _database = database;
         _subscribe(database);

         RefreshHostAlerts();

         foreach (KeyValuePair<string, IReadOnlyList<IAlert>> pair in database.CoreAlerts)
         {
            _store(pair.Key, pair.Value);
         }

         _raiseChanged();

         if (database.User is not null)
         {
            database.RefreshAlerts();
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

      public void RefreshHostAlerts()
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

         IReadOnlyList<IAlert> alerts = issues == HostSecurityIssue.None
            ? []
            : [new HostSecuritySettingsAlert(issues)];

         _store(AlertKinds.HostSecuritySettings, alerts);
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
         database.CoreAlertsChanged += _onKindChanged;
         database.CoreAlertsScanCompleted += _onScanCompleted;
      }

      private void _unsubscribe(IDatabase database)
      {
         database.CoreAlertsChanged -= _onKindChanged;
         database.CoreAlertsScanCompleted -= _onScanCompleted;
      }

      private void _onKindChanged(object? sender, AlertsChangedEventArgs e)
      {
         _store(e.Kind, e.Alerts);
      }

      private void _onScanCompleted(object? sender, EventArgs e)
      {
         // Host posture can change while a scan runs (filter attach/detach).
         RefreshHostAlerts();
         _raiseChanged();
      }

      private void _store(string kind, IReadOnlyList<IAlert> alerts)
      {
         lock (_gate)
         {
            _byKind[kind] = alerts;
         }
      }

      private void _raiseChanged()
         => NotifiedAlertsChanged?.Invoke(this, EventArgs.Empty);
   }
}
