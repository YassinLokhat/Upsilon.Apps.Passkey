using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.MAUI.Views;
using Upsilon.Apps.Passkey.GUI.WPF.Views;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace MAUI
{
    public partial class MainPage : ContentPage
    {
        private MainViewModel _viewModel;
        private bool _isPasswordStep = false;

        private IDispatcherTimer _timer = null!;
        private readonly TimeSpan _timeoutDuration = TimeSpan.FromMinutes(2);

        public MainPage()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            BindingContext = _viewModel;

            _timer = Dispatcher.CreateTimer();
            _timer.Interval = _timeoutDuration;
            _timer.Tick += _timer_Tick;
            _timer.Start();

#if WINDOWS
            Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping("GlobalKeyInterceptor", (handler, view) =>
            {
                var nativeWindow = handler.PlatformView;
                nativeWindow.Content.KeyDown += (sender, e) =>
                {
                    if (e.Key == Windows.System.VirtualKey.Escape)
                    {
                        ResetToUsernameStep();
                    }
                };
            });
#endif
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            _resetCredentials();
            MainViewModel.Database?.Close();
            MainViewModel.Database = null;
        }

        private async void _onEntryCompleted(object sender, EventArgs e)
        {
            await ExecuteAuthenticationStepAsync();
        }

        private async void _onLoginButtonClicked(object sender, EventArgs e)
        {
            await ExecuteAuthenticationStepAsync();
        }

        private async Task ExecuteAuthenticationStepAsync()
        {
            _timer.Stop();

            // --------------------------------------------------
            // ÉTAPE 1 : NOM D'UTILISATEUR & DATABASE.OPEN
            // --------------------------------------------------
            if (!_isPasswordStep)
            {
                string username = _credentialEntry.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(username))
                {
                    _timer.Start();
                    return;
                }

                // 1. Définition du chemin du fichier .pku si non défini via FilePicker
                if (string.IsNullOrEmpty(_viewModel.DatabaseFile))
                {
                    string filename = MainViewModel.CryptographyCenter.GetHash(username);
                    _viewModel.DatabaseFile = Path.Combine(FileSystem.AppDataDirectory, $"{filename}.pku");
                }

                // 2. Vérification de l'existence du fichier
                if (!File.Exists(_viewModel.DatabaseFile))
                {
                    await DisplayAlert("Erreur", $"Aucune base de données trouvée pour l'utilisateur '{username}'.", "OK");
                    _timer.Start();
                    return;
                }

                // 3. Fermeture de toute session résiduelle
                if (MainViewModel.Database is not null)
                {
                    MainViewModel.Database.Close();
                    MainViewModel.Database = null;
                }

                // 4. Ouverture de la base avec les 5 services et le USERNAME
                try
                {
                    MainViewModel.Database = await Task.Run(() => Database.Open(
                        MainViewModel.CryptographyCenter,
                        MainViewModel.SerializationCenter,
                        MainViewModel.PasswordFactory,
                        MainViewModel.ClipboardManager,
                        _viewModel.DatabaseFile,
                        username));

                    MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
                    MainViewModel.Database.AutoSaveDetected += _database_AutoSaveDetected;

                    // Passage de l'interface en mode Mot de Passe
                    _viewModel.CredentialsLabel = "Password :";
                    _credentialEntry.Text = string.Empty;
                    _credentialEntry.Placeholder = "Saisir votre mot de passe...";
                    _credentialEntry.IsPassword = true; // Masque la saisie
                    _isPasswordStep = true;

                    SetEntryFocus();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Erreur", $"Impossible d'ouvrir la base de données : {ex.Message}", "OK");
                }
            }
            // --------------------------------------------------
            // ÉTAPE 2 : MOT DE PASSE & DATABASE.LOGIN
            // --------------------------------------------------
            else
            {
                string password = _credentialEntry.Text ?? string.Empty;

                if (string.IsNullOrEmpty(password))
                {
                    _timer.Start();
                    return;
                }

                if (MainViewModel.Database is not null)
                {
                    try
                    {
                        // Déchiffrement + Désérialisation JSON
                        await Task.Run(() => MainViewModel.Database.Login(password));

                        if (MainViewModel.Database.User is not null)
                        {
                            // SUCCÈS
                            await DisplayAlert("Succès", $"Bienvenue {MainViewModel.Database.User.Username} !", "OK");
                            _resetCredentials();
                        }
                        else
                        {
                            await DisplayAlert("Erreur", "Mot de passe incorrect, ou nom d'utilisateur ne correspondant pas à ce fichier.", "OK");
                            _resetCredentials();
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // Déchiffrement invalide (Mot de passe incorrect qui génère un JSON illisible)
                        await DisplayAlert("Erreur", "Mot de passe incorrect ou fichier de base corrompu.", "OK");
                        _resetCredentials();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Erreur d'authentification", ex.Message, "OK");
                        _resetCredentials();
                    }
                }

                else
                {
                    _resetCredentials();
                }

                _credentialEntry.Text = string.Empty;
            }

            _timer.Start();
        }

        // Événement déclenché lors du clic sur le bouton "Valider" ou "Entrée" du clavier virtuel
        private async void OnCredentialCompleted(object sender, EventArgs e)
        {
            await ExecuteAuthenticationStepAsync();
        }

        // Méthode pour annuler / réinitialiser (Équivalent de la touche ESC)
        private void OnCancelOrReset()
        {
            _resetCredentials();
            MainViewModel.Database?.Close();
            MainViewModel.Database = null;
        }
        private void ResetToUsernameStep()
        {
            _resetCredentials();
            MainViewModel.Database?.Close();
            MainViewModel.Database = null;

            _isPasswordStep = false;
            _viewModel.CredentialsLabel = "Username :";
            _credentialEntry.Text = string.Empty;
            _credentialEntry.Placeholder = "Saisir votre identifiant...";
            _credentialEntry.IsPassword = false;
            _loginButton.Text = "Suivant";

            _timer.Stop();
            _timer.Start();
        }

        private void _resetCredentials()
        {
            _viewModel.DatabaseFile = string.Empty;
            _viewModel.CredentialsLabel = "Username :";

            _isPasswordStep = false;
            _credentialEntry.Text = string.Empty;
            _credentialEntry.Placeholder = "Saisir votre identifiant...";
            _credentialEntry.IsPassword = false;
            _loginButton.Text = "Suivant";
            _timer.Stop();

            SetEntryFocus();
        }

        private void SetEntryFocus()
        {
            // Évite de bloquer la boucle graphique Android lors de la demande de focus
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () =>
            {
                if (_credentialEntry != null && _credentialEntry.IsLoaded)
                {
                    _credentialEntry.Focus();
                }
            });
        }

        private async void _onNavigateToRegisterPage_Click(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new UserSettingsView());
        }

        private void _database_DatabaseClosed(object? sender, LogoutEventArgs e)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _resetCredentials();
                    MainViewModel.Database = null;
                });
            }
            catch { }
        }

        private async void _database_AutoSaveDetected(object? sender, AutoSaveDetectedEventArgs e)
        {
            try
            {
                string action = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    return await DisplayActionSheet(
                        "Autosave detected",
                        "Cancel (Ignore and keep save file)",
                        null,
                        "Yes (Apply these changes)",
                        "No (Discard them)"
                    );
                });

                e.MergeBehavior = action switch
                {
                    "Cancel (Ignore and keep save file)" => AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile,
                    "No (Discard them)" => AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile,
                    _ => AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile
                };
            }
            catch { }
        }

        private async void _openDatabase_MenuItem_Click(object sender, EventArgs e)
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "com.upsilon.pku" } },
                { DevicePlatform.Android, new[] { "*/*" }},
                { DevicePlatform.WinUI, new[] { ".pku" } },
                { DevicePlatform.MacCatalyst, new[] { "pku" } }
            });

            PickOptions options = new()
            {
                PickerTitle = "Open user database file",
                FileTypes = customFileType
            };
            try
            {
                var result = await FilePicker.Default.PickAsync(options);
                if (result == null) return;

                // Vérification de l'extension pour Android (*/* accepte tout)
                if (!result.FileName.EndsWith(".pku", StringComparison.OrdinalIgnoreCase))
                {
                    await DisplayAlert("Fichier invalide", "Veuillez sélectionner un fichier avec l'extension .pku", "OK");
                    return;
                }

                _resetCredentials();
                MainViewModel.Database?.Close();
                MainViewModel.Database = null;
                string targetFilePath = result.FullPath;

                // Gestion spécifique Android/iOS : lecture via Stream vers le stockage local
                if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    string localPath = Path.Combine(FileSystem.CacheDirectory, result.FileName);

                    using (var sourceStream = await result.OpenReadAsync())
                    using (var targetStream = File.Create(localPath))
                    {
                        await sourceStream.CopyToAsync(targetStream);
                    }

                    targetFilePath = localPath;
                }

                _viewModel.DatabaseFile = targetFilePath;

                bool exists = File.Exists(_viewModel.DatabaseFile);
                await DisplayAlert("Succès", $"Fichier prêt !\nChemin : {_viewModel.DatabaseFile}\nExiste : {exists}", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur sélection fichier: {ex.Message}");
                // Correction: DisplayAlert au lieu de DisplayAlertAsync
                await DisplayAlert("Erreur", "Impossible d'ouvrir le fichier.", "OK");
            }
        }

        private async void _generatePassword_MenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var generatorPage = new PasswordGenerator();

#if WINDOWS || MACCATALYST
                var toolWindow = new Window(generatorPage)
                {
                    Title = "Password Generator",
                    Width = 500,
                    Height = 450,
                    MinimumWidth = 500,
                    MinimumHeight = 450
                };
                Application.Current?.OpenWindow(toolWindow);
#else
                await Navigation.PushModalAsync(generatorPage);
#endif
            }
            catch (Exception ex)
            {
                // Correction: DisplayAlert au lieu de DisplayAlertAsync
                await DisplayAlert("Erreur", ex.Message, "OK");
            }
        }

        private async void _newUser_MenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var userSettingsView = new UserSettingsView();
                var navigationContainer = new NavigationPage(userSettingsView);

#if WINDOWS || MACCATALYST
                double targetWidth = 900;
                double targetHeight = 800;

                var toolWindow = new Window(navigationContainer)
                {
                    Title = "User Settings",
                    Width = targetWidth,
                    Height = targetHeight,
                    MinimumWidth = targetWidth,
                    MinimumHeight = targetHeight
                };

                toolWindow.Created += (s, args) =>
                {
#if WINDOWS
                    var platformWindow = toolWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                    if (platformWindow is null) return;

                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(
                        WinRT.Interop.WindowNative.GetWindowHandle(platformWindow));

                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                    if (appWindow is not null)
                    {
                        var displayArea = Microsoft.UI.Windowing.DisplayArea.Primary;
                        var screenWidth = displayArea.WorkArea.Width;
                        var screenHeight = displayArea.WorkArea.Height;

                        int centerX = (int)((screenWidth - targetWidth) / 2);
                        int centerY = (int)((screenHeight - targetHeight) / 2);

                        appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
                    }
#endif
                };

                Application.Current?.OpenWindow(toolWindow);
#else
                await Navigation.PushModalAsync(navigationContainer);
#endif
            }
            catch (Exception ex)
            {
                // Correction: DisplayAlert au lieu de DisplayAlertAsync
                await DisplayAlert("Erreur", ex.Message, "OK");
            }
        }
    }
}