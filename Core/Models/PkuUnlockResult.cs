namespace Upsilon.Apps.Passkey.Core.Models
{
    public enum PkuUnlockResultType
    {
        Success,                
        BiometricFailed,        
        MasterPasswordRequired, 
        WrongPassword           
    }

    public class PkuUnlockResult
    {
        public PkuUnlockResultType ResultType { get; set; }
        public string? DecryptedContent { get; set; }
    }
}