namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// Narrow surface ActivityCenter needs from its owning vault. Keeps the
   /// association unidirectional (<c>Database</c> → <c>ActivityCenter</c>) so
   /// ActivityCenter does not dig into Database members (CodeQL
   /// <c>cs/coupled-types</c>).
   /// </summary>
   internal interface IActivityHost
   {
      bool IsLoggedIn { get; }

      /// <summary>
      /// Trusted seal watermark stored in the tamper-proof database entry.
      /// Meaningful only when <see cref="IsLoggedIn"/> is true.
      /// </summary>
      int ActivitySealWatermark { get; }

      /// <summary>
      /// Months of activity history to keep; <c>0</c> means no pruning.
      /// Meaningful only when <see cref="IsLoggedIn"/> is true.
      /// </summary>
      int ActivityRetentionMonths { get; }

      string EncryptActivity(string plaintext, string publicKey);

      /// <summary>
      /// Decrypts one activity ciphertext with the logged-in private key.
      /// Throws when not logged in; crypto failures propagate to the caller.
      /// </summary>
      string DecryptActivity(string ciphertext);

      /// <summary>
      /// Public key that belongs to the logged-in private key (anchors seal checks).
      /// </summary>
      string GetTrustedPublicKey();

      bool VerifySeal(string canonical, string signature, string trustedPublicKey);

      string SignSeal(string canonical);

      void SaveActivityLog(ActivityCenter activityCenter);
   }
}
