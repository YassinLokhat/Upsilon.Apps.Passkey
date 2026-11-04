using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interface.Services;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Services
{
    public class PkuLifecycleManager
    {
        private readonly ICryptographyCenter _cryptoCenter;
        private readonly IBiometricAuthService _biometricService;
        private readonly ISecureStorageService _secureStorage;

        private const string MasterKeyAlias = "pku_stored_master_password";

        public PkuLifecycleManager(
            ICryptographyCenter cryptoCenter,
            IBiometricAuthService biometricService,
            ISecureStorageService secureStorage)
        {
            _cryptoCenter = cryptoCenter;
            _biometricService = biometricService;
            _secureStorage = secureStorage;
        }

        public async Task<PkuUnlockResult> TryUnlockWithBiometricsAsync(string pkuEncryptedContent)
        {
            string? storedPassword = await GetStoredPasswordSafeAsync();
            if (string.IsNullOrEmpty(storedPassword))
            {
                return new PkuUnlockResult { ResultType = PkuUnlockResultType.MasterPasswordRequired };
            }

            bool authenticated = await _biometricService.AuthenticateAsync("Scannez votre empreinte pour déverrouiller votre fichier .pku");
            if (!authenticated)
            {
                return new PkuUnlockResult { ResultType = PkuUnlockResultType.BiometricFailed };
            }

            try
            {
                string decrypted = _cryptoCenter.DecryptSymmetrically(pkuEncryptedContent, new[] { storedPassword });
                return new PkuUnlockResult
                {
                    ResultType = PkuUnlockResultType.Success,
                    DecryptedContent = decrypted
                };
            }
            catch
            {
                ClearStoredPassword();
                return new PkuUnlockResult { ResultType = PkuUnlockResultType.MasterPasswordRequired };
            }
        }

        public PkuUnlockResult UnlockWithManualPassword(string pkuEncryptedContent, string manualMasterPassword)
        {
            try
            {
                string decrypted = _cryptoCenter.DecryptSymmetrically(pkuEncryptedContent, new[] { manualMasterPassword });
                return new PkuUnlockResult
                {
                    ResultType = PkuUnlockResultType.Success,
                    DecryptedContent = decrypted
                };
            }
            catch
            {
                return new PkuUnlockResult { ResultType = PkuUnlockResultType.WrongPassword };
            }
        }

        public async Task RegisterDeviceBiometricsAsync(string validMasterPassword)
        {
            bool isAvailable = await _biometricService.IsBiometricAvailableAsync();
            if (!isAvailable) return;

            bool authenticated = await _biometricService.AuthenticateAsync("Validez votre empreinte pour l'associer à cet appareil");
            if (authenticated)
            {
                await _secureStorage.SetAsync(MasterKeyAlias, validMasterPassword);
            }
        }

        private async Task<string?> GetStoredPasswordSafeAsync()
        {
            try
            {
                return await _secureStorage.GetAsync(MasterKeyAlias);
            }
            catch
            {
                _secureStorage.Remove(MasterKeyAlias);
                return null;
            }
        }

        public void ClearStoredPassword()
        {
            _secureStorage.Remove(MasterKeyAlias);
        }
    }
}