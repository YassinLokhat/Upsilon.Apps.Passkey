using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Alerts
{
   /// <summary>
   /// Aggregates Core per-kind alert events and host posture alerts, then
   /// applies the user's <see cref="ISettings.AlertsToNotify"/> filter.
   /// <see cref="NotifiedAlertsChanged"/> is raised on the UI thread when a
   /// dispatcher is configured.
   /// </summary>
   internal sealed class AlertBroker : IDisposable
   {
      private readonly Lock _gate = new();
      private readonly Dictionary<string, IReadOnlyList<IAlert>> _byKind = new(StringComparer.Ordinal);
      private readonly Func<HostSecurityIssue> _hostPosture;
      private IDatabase? _database;
      private bool _disposed;

      public AlertBroker(Func<HostSecurityIssue> hostPosture)
      {
         _hostPosture = hostPosture ?? throw new ArgumentNullException(nameof(hostPosture));
      }

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
               if (mask.Contains(pair.Key))
               {
                  result.AddRange(pair.Value);
               }
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

      public static AlertSeverity MaxSeverity(IEnumerable<IAlert> alerts)
      {
         IAlert[] alertsArray = [.. alerts];
         return alertsArray.Length != 0 ? alertsArray.Max(x => x.Severity) : AlertSeverity.Info;
      }

      public void Attach(IDatabase database)
      {
         ArgumentNullException.ThrowIfNull(database);
         Detach();

         lock (_gate)
         {
            _database = database;
         }

         database.CoreAlertsChanged += _onKindChanged;
         database.CoreAlertsScanCompleted += _onScanCompleted;

         // Read the snapshot outside the gate (Core takes its own lock). Kinds
         // already stored came from events raised after subscribing, so they
         // are newer than the snapshot and must not be overwritten.
         IReadOnlyDictionary<string, IReadOnlyList<IAlert>> snapshot = database.CoreAlerts;
         lock (_gate)
         {
            if (ReferenceEquals(_database, database))
            {
               foreach (KeyValuePair<string, IReadOnlyList<IAlert>> pair in snapshot)
               {
                  _ = _byKind.TryAdd(pair.Key, pair.Value);
               }
            }
         }

         RefreshHostAlerts();

         if (database.User is not null)
         {
            database.RefreshAlerts();
         }
      }

      public void Detach()
      {
         lock (_gate)
         {
            if (_database is { } database)
            {
               database.CoreAlertsChanged -= _onKindChanged;
               database.CoreAlertsScanCompleted -= _onScanCompleted;
               _database = null;
            }

            _byKind.Clear();
         }
      }

      public void RefreshHostAlerts()
      {
         HostSecurityIssue issues = _hostPosture();

         IReadOnlyList<IAlert> alerts = issues == HostSecurityIssue.None
            ? []
            : [new HostSecuritySettingsAlert(issues)];

         lock (_gate)
         {
            _byKind[AlertKinds.HostSecuritySettings] = alerts;
         }

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
         NotifiedAlertsChanged = null;
      }

      private void _onKindChanged(object? sender, AlertsChangedEventArgs e)
      {
         lock (_gate)
         {
            // Late event from a database that was detached meanwhile.
            if (!ReferenceEquals(sender, _database))
            {
               return;
            }

            _byKind[e.Kind] = e.Alerts;
         }
      }

      private void _onScanCompleted(object? sender, EventArgs e)
      {
         lock (_gate)
         {
            if (!ReferenceEquals(sender, _database))
            {
               return;
            }
         }

         // Host posture can change while a scan runs (filter attach/detach).
         RefreshHostAlerts();
      }

      private void _raiseChanged()
      {
         if (NotifiedAlertsChanged is null)
         {
            return;
         }

         UiThread.Post(() => NotifiedAlertsChanged?.Invoke(this, EventArgs.Empty));
      }
   }
}
