using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Interfaces.Events
{
   /// <summary>
   /// Snapshot for one warning kind after a scan (unfiltered). May be raised
   /// from a worker thread.
   /// </summary>
   public sealed class WarningsChangedEventArgs : EventArgs
   {
      public WarningsChangedEventArgs(string kind, IReadOnlyList<IWarning> warnings)
      {
         Kind = kind ?? throw new ArgumentNullException(nameof(kind));
         Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
      }

      public string Kind { get; }

      public IReadOnlyList<IWarning> Warnings { get; }
   }
}
