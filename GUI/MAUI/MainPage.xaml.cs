using System.IO;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace MAUI
{
    public partial class MainPage : ContentPage
    {
        private MainViewModel _viewModel;
        private string? _username;
        private string? _p1;

        public MainPage()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            BindingContext = _viewModel;
        }

        private async void _openDatabase_MenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Sélection du fichier
                var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Ouvrir .pku" });
                if (result == null) return;

                // 2. Copie locale
                string localPath = Path.Combine(FileSystem.AppDataDirectory, result.FileName);
                using (var sourceStream = await result.OpenReadAsync())
                using (var targetStream = File.Create(localPath))
                {
                    await sourceStream.CopyToAsync(targetStream);
                }

                // 3. Identification
                string? username = await DisplayPromptAsync("Connexion", "Utilisateur :");
                if (string.IsNullOrWhiteSpace(username)) return;

                string? p1 = await DisplayPromptAsync("Sécurité 1/2", "Mot de passe 1 :", keyboard: Keyboard.Password);
                if (p1 == null) return;

                string? p2 = await DisplayPromptAsync("Sécurité 2/2", "Mot de passe 2 :", keyboard: Keyboard.Password);
                if (p2 == null) return;

                // 4. Ouverture du fichier
                MainViewModel.Database = Upsilon.Apps.Passkey.Core.Models.Database.Open(
                    MainViewModel.CryptographyCenter,
                    MainViewModel.SerializationCenter,
                    MainViewModel.PasswordFactory,
                    MainViewModel.ClipboardManager,
                    localPath,
                    username);

                if (MainViewModel.Database == null)
                {
                    await DisplayAlertAsync("Erreur", "Impossible de charger le fichier.", "Fermer");
                    return;
                }

                // 5. LOGIN — Un appel par mot de passe séparément
                try
                {
                    IUser? user = MainViewModel.Database.Login(p1); // Retourne null, normal
                    user = MainViewModel.Database.Login(p2);         // Retourne IUser si correct

                    if (user != null)
                    {
                        _viewModel.DatabaseLabel = $"🟢 Connecté : {result.FileName}";
                        await DisplayAlertAsync("Succès", $"Bienvenue {user.Username} !", "OK");
                    }
                    else
                    {
                        await DisplayAlertAsync("Échec", "Mot de passe incorrect.", "Fermer");
                        MainViewModel.Database = null;
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Erreur Login",
                        $"Type: {ex.GetType().Name}\nMessage: {ex.Message}",
                        "Fermer");
                    MainViewModel.Database = null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erreur Technique",
                    $"{ex.GetType().Name}: {ex.Message}",
                    "Fermer");
            }
        }

        // Entry field — 3 étapes : username → p1 → p2 → login
        private async void OnUsernameCompleted(object sender, EventArgs e)
        {
            string value = _mainEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(value))
            {
                await DisplayAlertAsync("Champ vide", "Veuillez saisir une valeur.", "OK");
                return;
            }

            // Étape 1 — Username
            if (!_mainEntry.IsPassword && _username == null)
            {
                _username = value;
                _mainEntry.Text = string.Empty;
                _mainEntry.IsPassword = true;
                _inputLabel.Text = "Mot de passe 1";
                _mainEntry.Placeholder = "Entrez le mot de passe 1";
                _mainEntry.Focus();
                return;
            }

            // Étape 2 — Mot de passe 1
            if (_mainEntry.IsPassword && _p1 == null)
            {
                _p1 = value;
                _mainEntry.Text = string.Empty;
                _inputLabel.Text = "Mot de passe 2";
                _mainEntry.Placeholder = "Entrez le mot de passe 2";
                _mainEntry.Focus();
                return;
            }

            // Étape 3 — Mot de passe 2 → Login
            if (_mainEntry.IsPassword && _p1 != null)
            {
                string p2 = value;
                await OnLoginSubmitted(_username!, _p1, p2);

                // Reset
                _username = null;
                _p1 = null;
                _mainEntry.Text = string.Empty;
                _mainEntry.IsPassword = false;
                _inputLabel.Text = "Username";
                _mainEntry.Placeholder = "Saisir ici...";
            }
        }

        private async Task OnLoginSubmitted(string username, string p1, string p2)
        {
            if (MainViewModel.Database == null)
            {
                await DisplayAlertAsync("Erreur", "Aucune base de données chargée.", "Fermer");
                return;
            }

            try
            {
                // Un appel Login() par mot de passe
                IUser? user = MainViewModel.Database.Login(p1);
                user = MainViewModel.Database.Login(p2);

                if (user != null)
                {
                    _viewModel.DatabaseLabel = $"🟢 Connecté : {username}";
                    await DisplayAlertAsync("Succès", $"Bienvenue {user.Username} !", "OK");
                }
                else
                {
                    await DisplayAlertAsync("Échec", "Mot de passe incorrect.", "Fermer");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erreur",
                    $"{ex.GetType().Name}: {ex.Message}",
                    "Fermer");
            }
        }
        private void _newUser_MenuItem_Click(object sender, EventArgs e) { /* Logique création */ }
    }
}