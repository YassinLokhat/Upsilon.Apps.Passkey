using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class AccountPasswordsWarningViewModel : INotifyPropertyChanged
   {
      public string Title { get; }

      public string ReadableWarningType
      {
         get => WarningType.ToReadableString();
         set => WarningType = EnumHelper.ActivityWarningTypeFromReadableString(value);
      }
      public WarningType WarningType
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               _onPropertyChanged(nameof(ReadableWarningType));
               RefreshFilters();
            }
         }
      } = WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning;
      public string Text
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               _onPropertyChanged(nameof(Text));
               RefreshFilters();
            }
         }
      } = "";

      public ObservableCollection<AccountPasswordWarningViewModel> Warnings { get; set; } = [];

      public ICommand ClearFiltersCommand { get; }

      public event PropertyChangedEventHandler? PropertyChanged;

      private void _onPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public AccountPasswordsWarningViewModel()
      {
         Title = AppInfo.Title + " - Account Passwords Warnings";
         ClearFiltersCommand = new RelayCommand(ClearFilters);
         RefreshFilters();
      }

      public void ClearFilters()
      {
         WarningType = WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning;
         Text = string.Empty;
      }

      public void RefreshFilters()
      {
         Warnings.Clear();

         if (AppServices.Session.Database?.Warnings is null) return;

         AccountPasswordWarningViewModel[] warnings = [.. AppServices.Session.Database.Warnings
            .Where(x => WarningType.HasFlag(x.WarningType))
            .SelectMany(x => x.Accounts?.Select(y => new AccountPasswordWarningViewModel(y, x.WarningType)) ?? [])
            .Where(x => x.MeetsConditions(WarningType, Text))];

         foreach (AccountPasswordWarningViewModel warning in warnings)
         {
            Warnings.Add(warning);
         }
      }
   }
}
