using Microsoft.Maui.Controls;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace MAUI
{
    public partial class UserSettingsView : ContentPage
    {
        public UserSettingsView()
        {
            InitializeComponent();
        }

        private async void OnRegisterSubmitted_Click(object sender, EventArgs e)
        {
            string username = _usernameEntry.Text?.Trim() ?? "";
            string password = _passwordEntry.Text ?? "";

            // 1. Validation des champs de saisie
            if (string.IsNullOrWhiteSpace(username))
            {
                await DisplayAlertAsync("Champ requis", "Veuillez entrer un nom d'utilisateur.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlertAsync("Champ requis", "Veuillez configurer un mot de passe principal.", "OK");
                return;
            }

            try
            {
                // 2. Préparation du fichier local propre à l'utilisateur
                string cleanName = username.ToLower().Trim();
                string fileName = $"{cleanName}_vault.pku";
                string localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                // 3. Encapsulation du mot de passe initial unique dans le tableau requis
                string[] initialPasskeys = [password];

                // 4. Appel à ton moteur de génération de base de données
                MainViewModel.Database = Upsilon.Apps.Passkey.Core.Models.Database.Create(
                    MainViewModel.CryptographyCenter,
                    MainViewModel.SerializationCenter,
                    MainViewModel.PasswordFactory,
                    MainViewModel.ClipboardManager,
                    localPath,
                    username,
                    initialPasskeys);

                if (MainViewModel.Database != null)
                {
                    await DisplayAlertAsync("Succès !", $"Votre coffre-fort '{fileName}' a été créé de manière sécurisée.", "Super");

                    // 5. Retour automatique vers l'écran de connexion principal
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlertAsync("Erreur", "Le moteur n'a pas pu initialiser la base de données.", "Fermer");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erreur de création", $"{ex.GetType().Name}: {ex.Message}", "Fermer");
            }
        }
    }
}