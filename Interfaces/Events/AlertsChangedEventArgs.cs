using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Interfaces.Events
{
   /// <summary>
   /// Snapshot for one alert kind after a scan (unfiltered). May be raised
   /// from a worker thread.
   /// </summary>
   public sealed class AlertsChangedEventArgs : EventArgs
   {
      public AlertsChangedEventArgs(string kind, IReadOnlyList<IAlert> alerts)
      {
         Kind = kind ?? throw new ArgumentNullException(nameof(kind));
         Alerts = alerts ?? throw new ArgumentNullException(nameof(alerts));
      }

      public string Kind { get; }

      public IReadOnlyList<IAlert> Alerts { get; }
   }
}
