using System;
using CommunityToolkit.Mvvm.Input;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;
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

        private void _timer_Tick(object sender, EventArgs e)
        {
            _resetCredentials();
            MainViewModel.Database?.Close();
            MainViewModel.Database = null;
        }
       
        private void _onEntryCompleted(object sender, EventArgs e)
        {
            ExecuteAuthenticationStep();
        }
        private void _onLoginButtonClicked(object sender, EventArgs e)
        {
            ExecuteAuthenticationStep();
        }

        private void ExecuteAuthenticationStep()
        {
            _timer.Stop();
            string inputText = _credentialEntry.Text;

            if (string.IsNullOrEmpty(inputText))
            {
                _timer.Start();
                return;
            }

            if (!_isPasswordStep)
            {
                if (!File.Exists(_viewModel.DatabaseFile))
                {
                    string filename = MainViewModel.CryptographyCenter.GetHash(inputText);
                    _viewModel.DatabaseFile = Path.Combine(FileSystem.AppDataDirectory, "raw", $"{filename}.pku");
                }

                try
                {
                    MainViewModel.Database = Database.Open(
                        MainViewModel.CryptographyCenter,
                        MainViewModel.SerializationCenter,
                        MainViewModel.PasswordFactory,
                        MainViewModel.ClipboardManager,
                        _viewModel.DatabaseFile,
                        inputText);

                    MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
                    MainViewModel.Database.AutoSaveDetected += _database_AutoSaveDetected;
                }
                catch { }

                _viewModel.CredentialsLabel = "Password :";
                _credentialEntry.Text = string.Empty;
                _credentialEntry.Placeholder = "Saisir votre mot de passe...";
                _credentialEntry.IsPassword = true; 
                _loginButton.Text = "Login";

                _isPasswordStep = true;
                _credentialEntry.Focus();
            }
            else
            {
                if (MainViewModel.Database is not null)
                {
                    _ = MainViewModel.Database.Login(inputText);

                    if (MainViewModel.Database.User is not null)
                    {
                        _resetCredentials();
                    }
                }
                _credentialEntry.Text = string.Empty;
            }
            _timer.Start();
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
            _credentialEntry.Focus();
            _loginButton.Text = "Suivant";
            _timer.Stop();
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
                    return await DisplayActionSheetAsync(
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
        { DevicePlatform.Android, new[] { "application/octet-stream" } },
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

                _resetCredentials();
                MainViewModel.Database?.Close();
                MainViewModel.Database = null;
                _viewModel.DatabaseFile = result.FullPath;            
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la sélection du fichier: {ex.Message}");
                await DisplayAlertAsync("Erreur", "Impossible d'ouvrir le fichier.", "OK");
            }
        }

        private void _GeneratePassword_MenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                int length = 20;

                // 1. Construction de l'alphabet
                System.Text.StringBuilder sb = new();
                sb.Append(MainViewModel.PasswordFactory.Numeric);
                sb.Append(MainViewModel.PasswordFactory.Alphabetic.ToUpper());
                sb.Append(MainViewModel.PasswordFactory.Alphabetic.ToLower());
                sb.Append(MainViewModel.PasswordFactory.SpecialChars);

                string alphabet = sb.ToString();

                // 2. Génération sécurisée (Tente d'utiliser ta Factory existante)
                string newPassword = MainViewModel.PasswordFactory.GeneratePassword(length, alphabet, checkIfLeaked: true);

                // 3. Assignation sur le thread principal UI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _credentialEntry.Text = newPassword;
                });
            }
            catch (PlatformNotSupportedException)
            {
                // 4. PLAN B : Si ta Factory crash sur cette plateforme, on utilise un générateur de secours 100% MAUI
                int lengthFallback = 20;
                string fallbackChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";

                System.Text.StringBuilder fallbackSb = new();
                Random rand = new();

                for (int i = 0; i < lengthFallback; i++)
                {
                    fallbackSb.Append(fallbackChars[rand.Next(fallbackChars.Length)]);
                }

                string fallbackPassword = fallbackSb.ToString();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _credentialEntry.Text = fallbackPassword;
                });
            }
            catch (Exception ex)
            {
                // En cas d'un autre type d'erreur, on affiche une alerte MAUI
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlertAsync("Erreur de génération", ex.Message, "OK");
                });
            }
        }
        private void _newUser_MenuItem_Click()
        {
            
        }
       
    }
}