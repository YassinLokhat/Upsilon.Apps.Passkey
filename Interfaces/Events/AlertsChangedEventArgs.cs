using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Interfaces.Events
{
   /// <summary>
   /// Snapshot for one alert kind after a scan (unfiltered). May be raised
   /// from a worker thread.
   /// </summary>
   public sealed class AlertsChangedEventArgs(string kind, IReadOnlyList<IAlert> alerts) : EventArgs
   {
      public string Kind { get; } = kind ?? throw new ArgumentNullException(nameof(kind));

      public IReadOnlyList<IAlert> Alerts { get; } = alerts ?? throw new ArgumentNullException(nameof(alerts));
   }
}
