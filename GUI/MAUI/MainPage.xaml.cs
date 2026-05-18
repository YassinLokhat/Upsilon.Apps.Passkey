using System.IO;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

using Upsilon.Apps.Passkey.Interfaces.Models;

namespace MAUI
{
    public partial class MainPage : ContentPage
    {
        private MainViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            BindingContext = _viewModel;
        }

        private async void OnNavigateToRegisterPage_Click(object sender, EventArgs e)
        {
           
            await Navigation.PushAsync(new UserSettingsView());
        }
        

        
        private async void _openDatabase_MenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Ouvrir votre fichier .pku" });
                if (result == null) return;

                string localPath = Path.Combine(FileSystem.AppDataDirectory, result.FileName);
                using (var sourceStream = await result.OpenReadAsync())
                using (var targetStream = File.Create(localPath))
                {
                    await sourceStream.CopyToAsync(targetStream);
                }

                string? username = await DisplayPromptAsync("Connexion", "Utilisateur :");
                if (string.IsNullOrWhiteSpace(username)) return;

                MainViewModel.Database = Upsilon.Apps.Passkey.Core.Models.Database.Open(
                    MainViewModel.CryptographyCenter,
                    MainViewModel.SerializationCenter,
                    MainViewModel.PasswordFactory,
                    MainViewModel.ClipboardManager,
                    localPath,
                    username);

                if (MainViewModel.Database == null)
                {
                    await DisplayAlertAsync("Erreur", "Impossible de lire ce fichier.", "Fermer");
                    return;
                }

               
                await ProcessDynamicLogin(username);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erreur Technique", $"{ex.GetType().Name}: {ex.Message}", "Fermer");
            }
        }

      
        private async Task ProcessDynamicLogin(string username)
        {
            if (MainViewModel.Database == null) return;

            try
            {
                IUser? user = null;
                int step = 1;

                
                while (true)
                {
                    string? pwd = await DisplayPromptAsync(
                        $"Authentification - Étape {step}",
                        $"Entrez le mot de passe n°{step} :",
                        keyboard: Keyboard.Password);

                    if (string.IsNullOrWhiteSpace(pwd))
                    {
                        MainViewModel.Database = null; // Annulation
                        return;
                    }

                    user = MainViewModel.Database.Login(pwd);

                    
                    if (user != null)
                    {
                        _viewModel.DatabaseLabel = $"🟢 Connecté : {username}";
                        await DisplayAlertAsync("Succès", $"Ravi de vous revoir {user.Username} !", "Accéder au coffre");
                        return;
                    }

                    
                    step++;
                }
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Échec", "Mot de passe incorrect ou erreur d'authentification.", "Fermer");
                MainViewModel.Database = null;
            }
        }

        
        private async void OnUsernameCompleted(object sender, EventArgs e)
        {
            string username = _mainEntry.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(username)) return;

            
            if (MainViewModel.Database == null)
            {
                await DisplayAlertAsync("Info", "Veuillez d'abord sélectionner ou ouvrir votre fichier de base.", "OK");
                return;
            }

            await ProcessDynamicLogin(username);
            _mainEntry.Text = string.Empty;
        }
    }
}