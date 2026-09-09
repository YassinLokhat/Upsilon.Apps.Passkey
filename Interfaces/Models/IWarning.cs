using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Minimal warning contract. Component-specific payloads live on typed
   /// interfaces that extend this one.
   /// </summary>
   public interface IWarning
   {
      /// <summary>Publishing component (e.g. <see cref="WarningKinds.SourceCore"/>).</summary>
      string Source { get; }

      /// <summary>Stable kind id (e.g. <see cref="WarningKinds.PasswordLeaked"/>).</summary>
      string Kind { get; }

      WarningSeverity Severity { get; }
   }
}
