using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class UserSettingsViewModel : INotifyPropertyChanged
   {
      public string Title { get; }
      public string Username
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = "NewUser";
      public int LogoutTimeout
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;

               _onPropertyChanged(nameof(LogoutTimeout));
               _onPropertyChanged(nameof(LogoutTimeoutChecked));
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
               _onPropertyChanged(nameof(LogoutTimeoutChecked));
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

               _onPropertyChanged(nameof(CleaningClipboardTimeout));
               _onPropertyChanged(nameof(CleaningClipboardTimeoutChecked));
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
               _onPropertyChanged(nameof(CleaningClipboardTimeoutChecked));
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
               _onPropertyChanged(nameof(ShowPasswordDelay));
               _onPropertyChanged(nameof(ShowPasswordDelayChecked));
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
               _onPropertyChanged(nameof(ShowPasswordDelayChecked));
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
               _onPropertyChanged(nameof(NumberOfOldPasswordToKeep));
               _onPropertyChanged(nameof(NumberOfOldPasswordToKeepChecked));
            }
         }
      }
      public bool NumberOfOldPasswordToKeepChecked
      {
         get => NumberOfOldPasswordToKeep != 0;
         set
         {
            if (NumberOfOldPasswordToKeepChecked != value)
            {
               NumberOfOldPasswordToKeep = value ? 10 : 0;
               _onPropertyChanged(nameof(NumberOfOldPasswordToKeepChecked));
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
               _onPropertyChanged(nameof(NumberOfMonthActivitiesToKeep));
               _onPropertyChanged(nameof(NumberOfMonthActivitiesToKeepChecked));
            }
         }
      }
      public bool NumberOfMonthActivitiesToKeepChecked
      {
         get => NumberOfMonthActivitiesToKeep != 0;
         set
         {
            if (NumberOfMonthActivitiesToKeepChecked != value)
            {
               NumberOfMonthActivitiesToKeep = value ? 12 : 0;
               _onPropertyChanged(nameof(NumberOfMonthActivitiesToKeepChecked));
            }
         }
      }
      public bool NotifyActivityReview
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = true;
      public bool NotifyPasswordUpdateReminder
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = true;
      public bool NotifyDuplicatedPasswords
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = true;
      public bool NotifyPasswordLeaked
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = true;

      // --- Application-level offline leak filter (not saved in the vault) ---

      public bool OfflineLeakFilterEnabled
      {
         get;
         set
         {
            if (field == value)
            {
               return;
            }

            field = value;
            _onPropertyChanged(nameof(OfflineLeakFilterEnabled));
         }
      }

      public string OfflineLeakFilterStatus
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = "Unknown";

      public bool OfflineLeakFilterBusy
      {
         get;
         set
         {
            if (field == value)
            {
               return;
            }

            field = value;
            _onPropertyChanged(nameof(OfflineLeakFilterBusy));
            _onPropertyChanged(nameof(OfflineLeakFilterIdle));
         }
      }

      public bool OfflineLeakFilterIdle => !OfflineLeakFilterBusy;

      public string OfflineLeakFilterProgress
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = string.Empty;

      public event PropertyChangedEventHandler? PropertyChanged;

      private void _onPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public UserSettingsViewModel()
      {
         Title = AppInfo.Title;

         if (AppServices.Session.Database?.User is not { } user)
         {
            Title += " - New user";
         }
         else
         {
            Title += " - User settings";

            Username = user.Username;

            LogoutTimeout = user.Settings.LogoutTimeout;
            CleaningClipboardTimeout = user.Settings.CleaningClipboardTimeout;
            ShowPasswordDelay = user.Settings.ShowPasswordDelay;
            NumberOfOldPasswordToKeep = user.Settings.NumberOfOldPasswordToKeep;
            NumberOfMonthActivitiesToKeep = user.Settings.NumberOfMonthActivitiesToKeep;

            NotifyActivityReview = (user.Settings.WarningsToNotify & Passkey.Interfaces.Enums.WarningType.ActivityReviewWarning) != 0;
            NotifyPasswordUpdateReminder = (user.Settings.WarningsToNotify & Passkey.Interfaces.Enums.WarningType.PasswordUpdateReminderWarning) != 0;
            NotifyDuplicatedPasswords = (user.Settings.WarningsToNotify & Passkey.Interfaces.Enums.WarningType.DuplicatedPasswordsWarning) != 0;
            NotifyPasswordLeaked = (user.Settings.WarningsToNotify & Passkey.Interfaces.Enums.WarningType.PasswordLeakedWarning) != 0;
         }

         RefreshOfflineLeakFilterStatus();
      }

      public void RefreshOfflineLeakFilterStatus()
      {
         Core.Utils.LeakFilter.LeakFilterConfig config = Core.Utils.LeakFilter.LeakFilterPaths.LoadConfig();
         OfflineLeakFilterEnabled = config.Enabled;

         string path = Core.Utils.LeakFilter.LeakFilterPaths.ResolveFilterFilePath(config);

         if (!System.IO.File.Exists(path))
         {
            OfflineLeakFilterStatus = $"Absent under {Core.Utils.LeakFilter.LeakFilterPaths.RootDirectory}";
            return;
         }

         System.IO.FileInfo info = new(path);
         string size = $"{info.Length / (1024d * 1024d * 1024d):0.00} GiB";
         string updated = info.LastWriteTimeUtc.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + " UTC";
         OfflineLeakFilterStatus = config.Enabled
            ? $"Present · {size} · updated {updated} · {path}"
            : $"Present on disk · {size} · updated {updated} · disabled · {path}";
      }
   }
}
