using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   public sealed partial class Database : IActivityHost
   {
      bool IActivityHost.IsLoggedIn => User is not null;

      int IActivityHost.ActivitySealWatermark => User?.ActivitySealWatermark
         ?? throw new NullValueException(nameof(User));

      int IActivityHost.ActivityRetentionMonths => User?.Settings.NumberOfMonthActivitiesToKeep
         ?? throw new NullValueException(nameof(User));

      string IActivityHost.EncryptActivity(string plaintext, string publicKey)
         => CryptographyCenter.EncryptAsymmetrically(plaintext, publicKey);

      string IActivityHost.DecryptActivity(string ciphertext)
      {
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         return CryptographyCenter.DecryptAsymmetrically(ciphertext, User.PrivateKey.Reveal());
      }

      string IActivityHost.GetTrustedPublicKey()
      {
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         return CryptographyCenter.GetPublicKey(User.PrivateKey.Reveal());
      }

      bool IActivityHost.VerifySeal(string canonical, string signature, string trustedPublicKey)
         => CryptographyCenter.Verify(canonical, signature, trustedPublicKey);

      string IActivityHost.SignSeal(string canonical)
      {
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         return CryptographyCenter.Sign(canonical, User.PrivateKey.Reveal());
      }

      void IActivityHost.SaveActivityLog(ActivityCenter activityCenter)
         => FileLocker.Save(activityCenter, ActivityFileEntry);
   }
}
