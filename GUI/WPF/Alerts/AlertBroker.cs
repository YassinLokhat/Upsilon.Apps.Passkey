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
      private readonly Lock _gate = new();
      private readonly Dictionary<string, IReadOnlyList<IAlert>> _byKind = new(StringComparer.Ordinal);
      private IDatabase? _database;
      private bool _disposed;

      // While User Settings is open, the alert toggles are shown live through this
      // mask instead of the saved one (the vault is never written for a preview).
      private AlertKindList? _previewMask;

      // The saved mask, read once per session (and again after the user settings
      // are saved): reading Settings goes through Touch, which would reset the
      // idle countdown every time an alert scan reports.
      private AlertKindList? _savedMask;
      private bool _savedMaskLoaded;

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
            if (!_savedMaskLoaded && _database?.User is { } user)
            {
               _savedMask = user.Settings.AlertsToNotify;
               _savedMaskLoaded = true;
            }

            AlertKindList? mask = _previewMask ?? _savedMask;
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
         IAlert[] alertsArray = [.. alerts];
         return alertsArray.Length != 0 ? alertsArray.Max(x => x.Severity) : AlertSeverity.Info;
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
         _previewMask = null;
         _savedMask = null;
         _savedMaskLoaded = false;

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

      /// <summary>
      /// Shows the alerts as if <paramref name="mask"/> were the user's saved
      /// <see cref="ISettings.AlertsToNotify"/>; <see langword="null"/> goes back to
      /// the saved one. Raises <see cref="NotifiedAlertsChanged"/>.
      /// </summary>
      public void SetPreviewMask(AlertKindList? mask)
      {
         lock (_gate)
         {
            _previewMask = mask;
         }

         _raiseChanged();
      }

      /// <summary>Re-reads the saved alert mask (after the user settings are saved).</summary>
      public void RefreshSavedMask()
      {
         lock (_gate)
         {
            _savedMaskLoaded = false;
         }

         _raiseChanged();
      }

      public void RefreshHostAlerts()
      {
         HostSecurityIssue issues = HostSecurityIssue.None;

         if (AppInfo.AppSettings.LoginIdleTimeoutSeconds <= 0)
         {
            issues |= HostSecurityIssue.IdleLoginDisabled;
         }

         if (!AppServices.PasswordFactory.HasLocalFilter
            && !AppServices.OfflineLeakFilterUpdate.IsBusy)
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
