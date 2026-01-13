using System.Collections.ObjectModel;
using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class AccountPasswordsWarningViewModel : INotifyPropertyChanged
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
               OnPropertyChanged(nameof(ReadableWarningType));
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
               OnPropertyChanged(nameof(Text));
               RefreshFilters();
            }
         }
      } = "";

      public ObservableCollection<AccountPasswordWarningViewModel> Warnings { get; set; } = [];

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public AccountPasswordsWarningViewModel()
      {
         Title = MainViewModel.AppTitle + " - Account Passwords Warnings";
         RefreshFilters();
      }

      public void RefreshFilters()
      {
         Warnings.Clear();

         if (MainViewModel.Database?.Warnings is null) return;

         AccountPasswordWarningViewModel[] warnings = [.. MainViewModel.Database.Warnings
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
