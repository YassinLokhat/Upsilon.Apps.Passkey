using Microsoft.Maui.Storage;
using Upsilon.Apps.Passkey.Interface.Services;

namespace Upsilon.Apps.Passkey.UI.Services
{
    public class MauiSecureStorageService : ISecureStorageService
    {
        public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);
        public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);
        public void Remove(string key) => SecureStorage.Default.Remove(key);
    }
}