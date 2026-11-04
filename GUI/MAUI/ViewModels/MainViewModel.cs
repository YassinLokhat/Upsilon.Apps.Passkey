using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Services;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.MAUI.OSSpecific;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public MainViewModel(PkuLifecycleManager pkuLifecycleManager)
        {
            _pkuLifecycleManager = pkuLifecycleManager;
        }
        public static string AppTitle
        {
            get
            {
                string name = AppInfo.Current.Name;
                string version = AppInfo.Current.Version.ToString(3);

                return $"{name} v{version}";
            }
        }

        public async Task OpenPkuFileAsync(string pkuEncryptedContent)
        {
            var result = await _pkuLifecycleManager.TryUnlockWithBiometricsAsync(pkuEncryptedContent);

            switch (result.ResultType)
            {
                case PkuUnlockResultType.Success:
                    OnPkuUnlocked(result.DecryptedContent);
                    break;

                case PkuUnlockResultType.MasterPasswordRequired:
                    await HandleManualFallbackAsync(pkuEncryptedContent);
                    break;

                case PkuUnlockResultType.BiometricFailed:
                    await Shell.Current.DisplayAlert("Information", "Authentification biométrique annulée.", "OK");
                    break;
            }
        }

        private async Task HandleManualFallbackAsync(string pkuEncryptedContent)
        {
            string password = await Shell.Current.DisplayPromptAsync(
                "Mot de passe maître",
                "Entrez votre mot de passe maître pour déverrouiller le fichier .pku :");

            if (string.IsNullOrEmpty(password)) return;

            var manualResult = _pkuLifecycleManager.UnlockWithManualPassword(pkuEncryptedContent, password);

            if (manualResult.ResultType == PkuUnlockResultType.Success)
            {
                OnPkuUnlocked(manualResult.DecryptedContent);

                bool enableBio = await Shell.Current.DisplayAlert(
                    "Empreinte digitale",
                    "Voulez-vous activer la connexion rapide par empreinte sur cet appareil ?",
                    "Oui", "Non");

                if (enableBio)
                {
                    await _pkuLifecycleManager.RegisterDeviceBiometricsAsync(password);
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Erreur", "Mot de passe maître incorrect.", "OK");
            }
        }

        private void OnPkuUnlocked(string? decryptedContent)
        {
            // Action to Perform with the Decrypted Data
        }

        public static readonly ICryptographyCenter CryptographyCenter = new CryptographyCenter();
        public static readonly ISerializationCenter SerializationCenter = new JsonSerializationCenter();
        public static readonly IPasswordFactory PasswordFactory = new PasswordFactory();
        public static readonly IClipboardManager ClipboardManager = new OSSpecificClipboardManager();
        private readonly PkuLifecycleManager _pkuLifecycleManager;

        public static IDatabase? Database = null;
        public static IUser User => Database?.User ?? throw new InvalidOperationException("Cannot access User because the database is not initialized.");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DatabaseLabel))] 
        private string _databaseFile = string.Empty;

        [ObservableProperty]
        private string _credentialsLabel = "Username :";

        public string DatabaseLabel => File.Exists(DatabaseFile)
            ? $"Database : {Path.GetFileName(DatabaseFile)}"
            : "No database loaded.";
    }

}
