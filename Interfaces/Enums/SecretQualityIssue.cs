namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Structural reasons a secret failed the local quality heuristic.
   /// </summary>
   [Flags]
   public enum SecretQualityIssue
   {
      None = 0,
      TooShort = 0b0001,
      LowDiversity = 0b0010,
      MatchesUsername = 0b0100,
      TrivialPattern = 0b1000,
   }
}
