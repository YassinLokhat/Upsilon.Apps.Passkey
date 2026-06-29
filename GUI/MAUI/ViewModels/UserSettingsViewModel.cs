using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Upsilon.Apps.Passkey.Interfaces.Enums;


namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
    [ObservableObject] 
    internal partial class UserSettingsViewModel
    {
        public string Title { get; }

        [ObservableProperty]
        private string _username = "NewUser";

        public int LogoutTimeout
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(LogoutTimeout));
                    OnPropertyChanged(nameof(LogoutTimeoutChecked));
                }
            }
        } = 5;

        public bool LogoutTimeoutChecked
        {
            get => LogoutTimeout != 0;
            set
            {
                if (LogoutTimeoutChecked != value)
                {
                    LogoutTimeout = value ? 5 : 0;
                    OnPropertyChanged(nameof(LogoutTimeoutChecked));
                }
            }
        }

        public int CleaningClipboardTimeout
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(CleaningClipboardTimeout));
                    OnPropertyChanged(nameof(CleaningClipboardTimeoutChecked));
                }
            }
        } = 30;

        public bool CleaningClipboardTimeoutChecked
        {
            get => CleaningClipboardTimeout != 0;
            set
            {
                if (CleaningClipboardTimeoutChecked != value)
                {
                    CleaningClipboardTimeout = value ? 30 : 0;
                    OnPropertyChanged(nameof(CleaningClipboardTimeoutChecked));
                }
            }
        }

        public int ShowPasswordDelay
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(ShowPasswordDelay));
                    OnPropertyChanged(nameof(ShowPasswordDelayChecked));
                }
            }
        } = 500;

        public bool ShowPasswordDelayChecked
        {
            get => ShowPasswordDelay != 0;
            set
            {
                if (ShowPasswordDelayChecked != value)
                {
                    ShowPasswordDelay = value ? 500 : 0;
                    OnPropertyChanged(nameof(ShowPasswordDelayChecked));
                }
            }
        }

        public int NumberOfOldPasswordToKeep
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(NumberOfOldPasswordToKeep));
                    OnPropertyChanged(nameof(NumberOfOldPasswordToKeepChecked));
                }
            }
        } = 0;

        public bool NumberOfOldPasswordToKeepChecked
        {
            get => NumberOfOldPasswordToKeep != 0;
            set
            {
                if (NumberOfOldPasswordToKeepChecked != value)
                {
                    NumberOfOldPasswordToKeep = value ? 10 : 0;
                    OnPropertyChanged(nameof(NumberOfOldPasswordToKeepChecked));
                }
            }
        }

        public int NumberOfMonthActivitiesToKeep
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(NumberOfMonthActivitiesToKeep));
                    OnPropertyChanged(nameof(NumberOfMonthActivitiesToKeepChecked));
                }
            }
        } = 0;

        public bool NumberOfMonthActivitiesToKeepChecked
        {
            get => NumberOfMonthActivitiesToKeep != 0;
            set
            {
                if (NumberOfMonthActivitiesToKeepChecked != value)
                {
                    NumberOfMonthActivitiesToKeep = value ? 12 : 0;
                    OnPropertyChanged(nameof(NumberOfMonthActivitiesToKeepChecked));
                }
            }
        }

        [ObservableProperty] private bool _notifyActivityReview = true;
        [ObservableProperty] private bool _notifyPasswordUpdateReminder = true;
        [ObservableProperty] private bool _notifyDuplicatedPasswords = true;
        [ObservableProperty] private bool _notifyPasswordLeaked = true;

        public UserSettingsViewModel()
        {
            Title = MainViewModel.AppTitle;

            if (MainViewModel.Database?.User is null)
            {
                Title += " - New user";
            }
            else
            {
                Title += " - User settings";

                _username = MainViewModel.Database.User.Username;
                LogoutTimeout = MainViewModel.Database.User.LogoutTimeout;
                CleaningClipboardTimeout = MainViewModel.Database.User.CleaningClipboardTimeout;
                ShowPasswordDelay = MainViewModel.Database.User.ShowPasswordDelay;
                NumberOfOldPasswordToKeep = MainViewModel.Database.User.NumberOfOldPasswordToKeep;
                NumberOfMonthActivitiesToKeep = MainViewModel.Database.User.NumberOfMonthActivitiesToKeep;

                _notifyActivityReview = (MainViewModel.Database.User.WarningsToNotify & WarningType.ActivityReviewWarning) != 0;
                _notifyPasswordUpdateReminder = (MainViewModel.Database.User.WarningsToNotify & WarningType.PasswordUpdateReminderWarning) != 0;
                _notifyDuplicatedPasswords = (MainViewModel.Database.User.WarningsToNotify & WarningType.DuplicatedPasswordsWarning) != 0;
                _notifyPasswordLeaked = (MainViewModel.Database.User.WarningsToNotify & WarningType.PasswordLeakedWarning) != 0;
            }
        }
    }
}