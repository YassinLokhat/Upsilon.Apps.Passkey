namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>App-level security posture issues (host / GUI).</summary>
   [Flags]
   public enum HostSecurityIssue
   {
      None = 0,
      IdleLoginDisabled = 0b0001,
      OfflineLeakFilterUnavailable = 0b0010,
   }
}
