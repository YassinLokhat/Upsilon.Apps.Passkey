using System.IO;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.MAUI.Helper;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
    public partial class UserSettingsView : ContentPage
    {
        private readonly UserSettingsViewModel _viewModel;
        private Task? _saveTask;
        private Task? _importTask;
        private Task? _exportTask;

        public UserSettingsView()
        {
            InitializeComponent();
            this.SizeChanged += UserSettingsView_SizeChanged;

            bool isDbActive = MainViewModel.Database?.User is not null;
            if (!isDbActive)
            {
                ToolbarItems.Clear();
            }

            BindingContext = _viewModel = new UserSettingsViewModel();

            if (MainViewModel.Database?.User is not null)
            {
                MainViewModel.User.Shake();
                MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
            }

            _username_Entry.Focus();
        }

        private void UserSettingsView_SizeChanged(object sender, EventArgs e)
        {
            if (_credentialsBorder is null || _settingsBorder is null)
            {
                return;
            }

            if (Width < 800)
            {
                // Mobile Mode: One below the other
                Grid.SetColumn(_credentialsBorder, 0);
                Grid.SetRow(_credentialsBorder, 0);

                Grid.SetColumn(_settingsBorder, 0);
                Grid.SetRow(_settingsBorder, 1);

                // On mobile, we let them take their natural height to scroll freely
                _credentialsBorder.VerticalOptions = LayoutOptions.Start;
                _settingsBorder.VerticalOptions = LayoutOptions.Start;
            }
            else
            {
                // Desktop Mode: Side by side with perfect alignment
                Grid.SetColumn(_credentialsBorder, 0);
                Grid.SetRow(_credentialsBorder, 0);

                Grid.SetColumn(_settingsBorder, 1);
                Grid.SetRow(_settingsBorder, 0);

                // Force stretching to equalize heights
                _credentialsBorder.VerticalOptions = LayoutOptions.Fill;
                _settingsBorder.VerticalOptions = LayoutOptions.Fill;
            }
        }

        private void _database_DatabaseClosed(object? sender, Interfaces.Events.LogoutEventArgs e)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopModalAsync();
                });
            }
            catch { }
        }

        private void Value_Entry_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.NewTextValue)) return;

            if (!System.Text.RegularExpressions.Regex.IsMatch(e.NewTextValue, "^[0-9]*$"))
            {
                ((Entry)sender).Text = e.OldTextValue;
            }
        }

        private string _canSave()
        {
            return string.IsNullOrEmpty(_viewModel.Username)
                ? "Username cannot be empty."
                : _passwordsContainer.Passkeys.Length == 0
                ? "At least one password should be set."
                : _passwordsContainer.Passkeys.Any(string.IsNullOrEmpty)
                ? "No password can be empty."
                : string.Empty;
        }

        private async void DeleteUser_Clicked(object sender, EventArgs e)
        {
            if (MainViewModel.Database?.User is null
                || !await DisplayAlertAsync("Confirmation required", "If you delete the user database, you will lost all credentials.\nAre you sure you want to delete the database anyway?", "Yes", "No")
                || !await DisplayAlertAsync("Confirmation required", "This procedure is non-reversible.\nPlease confirm to proceed the deletion.", "Yes", "No"))
            {
                return;
            }

            string databaseDirectory = Path.GetDirectoryName(MainViewModel.Database.DatabaseFile) ?? string.Empty;

            MainViewModel.Database.Delete();

            if (Directory.Exists(databaseDirectory))
            {
                Directory.Delete(databaseDirectory, true);
            }

            await DisplayAlertAsync("Success", $"'{_viewModel.Username}' user database deleted successfully", "OK");
            await Navigation.PopModalAsync();
        }

        private async Task _save()
        {
            string error = _canSave();
            if (!string.IsNullOrEmpty(error))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlertAsync("Error", error, "OK");
                });
                return;
            }

            string newFilename = MainViewModel.CryptographyCenter.GetHash(_viewModel.Username);
            string appDataPath = FileSystem.Current.AppDataDirectory;
            string newDatabaseFile = Path.Combine(appDataPath, "raw", $"{newFilename}.pku");

            bool newUser = false;
            bool credentialsChanged = false;
            string oldDatabaseFile = string.Empty;

            if (MainViewModel.Database?.User is null)
            {
                try
                {
                    bool useDefault = await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlertAsync("Use default location?", $"Use default database location :\n{newDatabaseFile}", "Yes", "No"));

                    if (!useDefault)
                    {
                  
                        var targetFolder = await CommunityToolkit.Maui.Storage.FolderPicker.Default.PickAsync(CancellationToken.None);
                        if (targetFolder.IsSuccessful)
                        {
                            newDatabaseFile = Path.Combine(targetFolder.Folder.Path, $"{newFilename}.pku");
                        }
                    }

                    MainViewModel.Database = Database.Create(MainViewModel.CryptographyCenter,
                        MainViewModel.SerializationCenter,
                        MainViewModel.PasswordFactory,
                        MainViewModel.ClipboardManager,
                        newDatabaseFile,
                        _viewModel.Username,
                        _passwordsContainer.Passkeys);

                    MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
                }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlertAsync("Error", ex.Message, "OK");
                    });
                    return;
                }

                newUser = true;
            }
            else
            {
                string oldFileName = MainViewModel.CryptographyCenter.GetHash(MainViewModel.Database.User.Username);
                oldDatabaseFile = Path.Combine(appDataPath, "raw", $"{oldFileName}.pku");

                credentialsChanged = _credentialsChanged(oldFileName,
                    oldPasskeys: MainViewModel.Database.User.Passkeys,
                    newFilename,
                    newPasskeys: _passwordsContainer.Passkeys);
            }

            if (MainViewModel.Database.User is not null)
            {
                MainViewModel.Database.User.Username = _viewModel.Username;
                MainViewModel.Database.User.Passkeys = _passwordsContainer.Passkeys;
                MainViewModel.Database.User.LogoutTimeout = _viewModel.LogoutTimeout;
                MainViewModel.Database.User.CleaningClipboardTimeout = _viewModel.CleaningClipboardTimeout;
                MainViewModel.Database.User.ShowPasswordDelay = _viewModel.ShowPasswordDelay;
                MainViewModel.Database.User.NumberOfOldPasswordToKeep = _viewModel.NumberOfOldPasswordToKeep;
                MainViewModel.Database.User.NumberOfMonthActivitiesToKeep = _viewModel.NumberOfMonthActivitiesToKeep;

                WarningType warningsToNotify = 0;
                if (_viewModel.NotifyActivityReview) warningsToNotify |= WarningType.ActivityReviewWarning;
                if (_viewModel.NotifyDuplicatedPasswords) warningsToNotify |= WarningType.DuplicatedPasswordsWarning;
                if (_viewModel.NotifyPasswordUpdateReminder) warningsToNotify |= WarningType.PasswordUpdateReminderWarning;
                if (_viewModel.NotifyPasswordLeaked) warningsToNotify |= WarningType.PasswordLeakedWarning;
                MainViewModel.Database.User.WarningsToNotify = warningsToNotify;

                MainViewModel.Database.Save();
            }

            string message = $"'{_viewModel.Username}' user database ";

            if (credentialsChanged)
            {
                message = $"'{_viewModel.Username}' user's credentials has been updated.\nYou will be logged out.\nPlease login again.";
                MainViewModel.Database.Close();

                string oldDatabaseDirectory = Path.GetDirectoryName(oldDatabaseFile) ?? string.Empty;
                string newDatabaseDirectory = Path.GetDirectoryName(newDatabaseFile) ?? string.Empty;

                if (oldDatabaseDirectory != newDatabaseDirectory)
                {
                    if (!Directory.Exists(newDatabaseDirectory))
                    {
                        _ = Directory.CreateDirectory(newDatabaseDirectory);
                    }

                    if (File.Exists(oldDatabaseFile))
                    {
                        File.Move(oldDatabaseFile, newDatabaseFile);
                    }

                    if (Directory.Exists(oldDatabaseDirectory))
                    {
                        Directory.Delete(oldDatabaseDirectory, true);
                    }
                }
            }
            else if (newUser)
            {
                message += "created successfully";
                MainViewModel.Database.Close();
            }
            else
            {
                message += "updated successfully";
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlertAsync("Success", message, "OK");
                await Navigation.PopModalAsync();
            });
        }

        private void Save_Clicked(object sender, EventArgs e)
        {
            if (_saveTask is null || _saveTask.IsCompleted)
            {
                _saveTask = Task.Run(async () => await _save());
            }
        }

        private static bool _credentialsChanged(string oldFileName, string[] oldPasskeys, string newFilename, string[] newPasskeys)
        {
            return oldFileName != newFilename || MainViewModel.SerializationCenter.AreDifferent(oldPasskeys, newPasskeys);
        }

        private async void Import_Clicked(object sender, EventArgs e)
        {
            if (MainViewModel.Database?.User is null) return;

            if (MainViewModel.Database.User.HasChanged()
                && !await DisplayAlertAsync("Import data", "Before importing data, all unsaved changes will be saved.", "OK", "Cancel"))
            {
                return;
            }

            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.comma-separated-values-text", "public.json" } },
                { DevicePlatform.Android, new[] { "text/comma-separated-values", "application/json" } },
                { DevicePlatform.WinUI, new[] { ".csv", ".json" } },
                { DevicePlatform.MacCatalyst, new[] { "csv", "json" } }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Import data from a file",
                FileTypes = customFileType
            });

            if (result == null) return;

            if (_importTask is null || _importTask.IsCompleted)
            {
                _importTask = Task.Run(() =>
                {
                    bool isSuccess = MainViewModel.Database.ImportFromFile(result.FullPath);

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (isSuccess)
                            await DisplayAlertAsync("Import success", "Import data has been completed successfully.\nMore details in the activities.", "OK");
                        else
                            await DisplayAlertAsync("Import failed", "Import data failed.\nMore details in the activities.", "OK");
                    });
                });
            }
        }

        private async void Export_Clicked(object sender, EventArgs e)
        {
            if (MainViewModel.Database?.User is null) return;

            if (MainViewModel.Database.User.HasChanged()
                && !await DisplayAlertAsync("Export data", "Before exporting data, all unsaved changes will be saved.", "OK", "Cancel"))
            {
                return;
            }
   
            string defaultFileName = $"{MainViewModel.Database.User.ItemId ?? string.Empty}-{DateTime.Now:yyyyMMddHHmm}.json";
            using var stream = new MemoryStream(); 

            var targetFile = await CommunityToolkit.Maui.Storage.FileSaver.Default.SaveAsync(defaultFileName, stream, CancellationToken.None);

            if (!targetFile.IsSuccessful) return;
  
            if (_exportTask is null || _exportTask.IsCompleted)
            {
                _exportTask = Task.Run(() =>
                {
                    bool isSuccess = MainViewModel.Database.ExportToFile(targetFile.FilePath);

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (isSuccess)
                            await DisplayAlertAsync("Export success", "Export data has been completed successfully.\nMore details in the activities.", "OK");
                        else
                            await DisplayAlertAsync("Export failed", "Export data failed.\nMore details in the activities.", "OK");
                    });
                });
            }
        }
    }
}