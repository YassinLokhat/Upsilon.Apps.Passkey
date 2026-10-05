using System.Diagnostics;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.MAUI.Views;
using Upsilon.Apps.Passkey.GUI.WPF.Views;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;

namespace MAUI
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IDispatcherTimer _timer;
        private bool _isPasswordStep = false;
        private bool _isBusy = false;
        private bool _sessionActive = false;
        private int _passwordAttempt = 0;

        public MainPage()
        {
            InitializeComponent();

            BindingContext = _mainViewModel = new MainViewModel();

            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMinutes(2);
            _timer.Tick += _timer_Elapsed;

            _resetCredentials();

#if WINDOWS
            Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping("GlobalKeyInterceptor", (handler, view) =>
            {
                handler.PlatformView.Content.KeyDown += (sender, e) =>
                {
                    if (e.Key == Windows.System.VirtualKey.Escape)
                    {
                        _closeDatabase();
                        _resetCredentials();
                    }
                };
            });
#endif
        }

        private void _timer_Elapsed(object? sender, EventArgs e)
        {
            
                if (_isBusy || _sessionActive) return;

                _closeDatabase();
                _resetCredentials();
            
        }

        private void _credential_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_sessionActive) return;
            _timer.Stop();
            _timer.Start();
        }

        private void _validate_Clicked(object? sender, EventArgs e)
        {
            _credential_Completed(sender, e);
        }

        private async void _credential_Completed(object? sender, EventArgs e)
        {
            if (_isBusy) return; // évite les doubles validations

            _isBusy = true;
            _timer.Stop();
            _setBusyUi(true);

            try
            {
                if (!_isPasswordStep)
                {
                    await _handleUsernameStepAsync();
                }
                else
                {
                    await _handlePasswordStepAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CRASH: {ex}");
                await DisplayAlert("Erreur", ex.Message, "OK");
            }
            finally
            {
                _setBusyUi(false);
                _isBusy = false;

                if (!_sessionActive)
                {
                    _timer.Start();
                }
            }
        }

        private void _setBusyUi(bool busy)
        {
            _busy.IsVisible = busy;
            _busy.IsRunning = busy;
            _username_TB.IsEnabled = !busy;
            _password_PB.IsEnabled = !busy;
            _validate_BTN.IsEnabled = !busy;
        }

        private async Task _handleUsernameStepAsync()
        {
            string username = _username_TB.Text ?? string.Empty;

            if (string.IsNullOrEmpty(username))
            {
                return;
            }

            if (!File.Exists(_mainViewModel.DatabaseFile))
            {
                string filename = MainViewModel.CryptographyCenter.GetHash(username);
                _mainViewModel.DatabaseFile = Path.Combine(FileSystem.AppDataDirectory, $"{filename}.pku");
            }

            if (!File.Exists(_mainViewModel.DatabaseFile))
            {
                await DisplayAlert("Erreur",
                    $"Aucune base de données trouvée pour l'utilisateur '{username}'.", "OK");
                _mainViewModel.DatabaseFile = string.Empty;
                return;
            }

            try
            {
                _closeDatabase();
                var sw = Stopwatch.StartNew();
                MainViewModel.Database = await Task.Run(() => Database.Open(
                    MainViewModel.CryptographyCenter,
                    MainViewModel.SerializationCenter,
                    MainViewModel.PasswordFactory,
                    MainViewModel.ClipboardManager,
                    _mainViewModel.DatabaseFile,
                    username));
                Debug.WriteLine($"OPEN took {sw.ElapsedMilliseconds} ms");

                MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
                MainViewModel.Database.AutoSaveDetected += _database_AutoSaveDetected;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Impossible d'ouvrir la base : {ex.Message}", "OK");
                return;
            }

            _isPasswordStep = true;
            _passwordAttempt = 0;
            _mainViewModel.CredentialsLabel = "Password :";

            _username_TB.Text = string.Empty;
            _username_TB.IsVisible = false;

            _password_PB.Text = string.Empty;
            _password_PB.IsVisible = true;
            _setFocus(_password_PB);
        }

        private async Task _handlePasswordStepAsync()
        {
            string password = _password_PB.Text ?? string.Empty;

            if (string.IsNullOrEmpty(password))
            {
                return;
            }

            if (MainViewModel.Database is null)
            {
                _resetCredentials();
                return;
            }

            try
            {
                await Task.Run(() => MainViewModel.Database.Login(password));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", ex.Message, "OK");
            }

            if (MainViewModel.Database?.User is not null)
            {
                _sessionActive = true;
                _timer.Stop();

                await DisplayAlert("Succès", $"Bienvenue {MainViewModel.Database.User.Username} !", "OK");
                _resetCredentials();
            }
            else
            {
                _passwordAttempt++;
                _mainViewModel.CredentialsLabel = $"Password {_passwordAttempt + 1} :";
                _password_PB.Text = string.Empty;
                _setFocus(_password_PB);
            }
        }

        private void _closeDatabase()
        {
            _sessionActive = false;

            var database = MainViewModel.Database;
            if (database is null) return;

            database.DatabaseClosed -= _database_DatabaseClosed;
            database.AutoSaveDetected -= _database_AutoSaveDetected;
            MainViewModel.Database = null;
            database.Close();
        }

        private void _resetCredentials()
        {
            _isPasswordStep = false;

            _mainViewModel.DatabaseFile = string.Empty;
            _mainViewModel.CredentialsLabel = "Username :";

            _username_TB.Text = string.Empty;
            _username_TB.IsVisible = true;

            _password_PB.Text = string.Empty;
            _password_PB.IsVisible = false;

            _timer.Stop();
            _setFocus(_username_TB);
        }

        private void _setFocus(Entry entry)
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () =>
            {
                if (entry.IsVisible && entry.IsLoaded)
                {
                    entry.Focus();
                }
            });
        }

        private void _reset_Clicked(object? sender, EventArgs e)
        {
            if (_isBusy) return;
            _closeDatabase();
            _resetCredentials();
        }

        private void _database_DatabaseClosed(object? sender, LogoutEventArgs e)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _sessionActive = false;
                    MainViewModel.Database = null;
                    _resetCredentials();
                });
            }
            catch { }
        }

        private void _database_AutoSaveDetected(object? sender, AutoSaveDetectedEventArgs e)
        {
            try
            {
                var tcs = new TaskCompletionSource<string>();

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        string action = await DisplayActionSheet(
                            "Autosave detected",
                            "Cancel (Ignore and keep save file)",
                            null,
                            "Yes (Apply these changes)",
                            "No (Discard them)");
                        tcs.TrySetResult(action);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                string result = tcs.Task.GetAwaiter().GetResult(); 

                e.MergeBehavior = result switch
                {
                    "Cancel (Ignore and keep save file)" => AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile,
                    "No (Discard them)" => AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile,
                    _ => AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile
                };
            }
            catch { }
        }

        private async void _openDatabase_MenuItem_Click(object? sender, EventArgs e)
        {
            if (_isBusy) return;
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "com.upsilon.pku" } },
                { DevicePlatform.Android, new[] { "*/*" } },
                { DevicePlatform.WinUI, new[] { ".pku" } },
                { DevicePlatform.MacCatalyst, new[] { "pku" } }
            });

            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Open user database file",
                    FileTypes = customFileType
                });
                if (result is null) return;

                if (!result.FileName.EndsWith(".pku", StringComparison.OrdinalIgnoreCase))
                {
                    await DisplayAlert("Fichier invalide", "Veuillez sélectionner un fichier .pku", "OK");
                    return;
                }

                _closeDatabase();
                _resetCredentials();

                string targetFilePath = result.FullPath;

                if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    string localPath = Path.Combine(FileSystem.CacheDirectory, result.FileName);

                    using (var source = await result.OpenReadAsync())
                    using (var target = File.Create(localPath))
                    {
                        await source.CopyToAsync(target);
                    }

                    targetFilePath = localPath;
                }

                _mainViewModel.DatabaseFile = targetFilePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur sélection fichier: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir le fichier.", "OK");
            }
        }

        private async void _generatePassword_MenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                var generatorPage = new PasswordGenerator();
#if WINDOWS || MACCATALYST
                Application.Current?.OpenWindow(new Window(generatorPage)
                {
                    Title = "Password Generator",
                    Width = 500,
                    Height = 450,
                    MinimumWidth = 500,
                    MinimumHeight = 450
                });
#else
                await Navigation.PushModalAsync(generatorPage);
#endif
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", ex.Message, "OK");
            }
        }

        private async void _newUser_MenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                var container = new NavigationPage(new UserSettingsView());
#if WINDOWS || MACCATALYST
                Application.Current?.OpenWindow(new Window(container)
                {
                    Title = "User Settings",
                    Width = 900,
                    Height = 800,
                    MinimumWidth = 900,
                    MinimumHeight = 800
                });
#else
                await Navigation.PushModalAsync(container);
#endif
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", ex.Message, "OK");
            }
        }
    }
}