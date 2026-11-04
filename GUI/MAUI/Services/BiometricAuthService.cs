using System;
using System.Threading;
using System.Threading.Tasks;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Upsilon.Apps.Passkey.Interface.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Services
{
    public class BiometricAuthService : IBiometricAuthService
    {
        public async Task<bool> IsBiometricAvailableAsync()
        {
            return await CrossFingerprint.Current.IsAvailableAsync();
        }

        public async Task<bool> AuthenticateAsync(string reason = "Veuillez vous authentifier par empreinte")
        {
            var request = new AuthenticationRequestConfiguration("Sécurité .pku", reason)
            {
                CancelTitle = "Annuler",
                FallbackTitle = "Mot de passe"
            };

            var result = await CrossFingerprint.Current.AuthenticateAsync(request);

            return result.Authenticated;
        }
    }
}