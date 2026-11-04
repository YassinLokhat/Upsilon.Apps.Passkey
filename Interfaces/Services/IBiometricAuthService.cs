namespace Upsilon.Apps.Passkey.Interface.Services
{
    public interface IBiometricAuthService
    {
        Task<bool> IsBiometricAvailableAsync();
        Task<bool> AuthenticateAsync(string reason = "Veuillez vous authentifier par empreinte");
    }
}