using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Minimal alert contract. Component-specific payloads live on typed
   /// interfaces that extend this one.
   /// </summary>
   public interface IAlert
   {
      /// <summary>Publishing component (e.g. <see cref="AlertKinds.SourceCore"/>).</summary>
      string Source { get; }

      /// <summary>Stable kind id (e.g. <see cref="AlertKinds.PasswordLeaked"/>).</summary>
      string Kind { get; }

      AlertSeverity Severity { get; }
   }
}
