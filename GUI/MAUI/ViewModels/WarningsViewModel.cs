using System.Collections.ObjectModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class WarningRowViewModel : ObservableObject
   {
      public WarningRowViewModel(string kind, string service, string account, string detail, IAccount? targetAccount)
      {
         Kind = kind;
         Service = service;
         Account = account;
         Detail = detail;
         TargetAccount = targetAccount;
      }

      public string Kind { get; }
      public string Service { get; }
      public string Account { get; }
      public string Detail { get; }
      public IAccount? TargetAccount { get; }
      public bool CanGoToAccount => TargetAccount is not null;
      public string Summary => $"{Kind}: {Service} / {Account} — {Detail}";
   }

   internal sealed class WarningsViewModel : ObservableObject
   {
      public WarningsViewModel()
      {
         RefreshCommand = new RelayCommand(Refresh);
         BackCommand = new AsyncRelayCommand(() => AppServices.Navigation.GoBackAsync());
         GoToAccountCommand = new AsyncRelayCommand(
            p => _goToAccountAsync(p as WarningRowViewModel),
            p => (p as WarningRowViewModel)?.TargetAccount is not null);
         Refresh();
      }

      public string Title => Strings.Title_Warnings;

      public ObservableCollection<WarningRowViewModel> Warnings { get; } = [];

      public WarningRowViewModel? SelectedWarning
      {
         get;
         set
         {
            if (SetProperty(ref field, value) && GoToAccountCommand is AsyncRelayCommand cmd)
            {
               cmd.NotifyCanExecuteChanged();
            }
         }
      }

      public ICommand RefreshCommand { get; }
      public ICommand BackCommand { get; }
      public ICommand GoToAccountCommand { get; }

      public void Refresh()
      {
         Warnings.Clear();
         IEnumerable<IWarning>? source = AppServices.Session.Database?.Warnings;
         if (source is null)
         {
            return;
         }

         foreach (IWarning warning in source)
         {
            if (warning.WarningType == WarningType.DuplicatedPasswordsWarning)
            {
               if (warning.Accounts is null)
               {
                  continue;
               }

               foreach (IAccount account in warning.Accounts)
               {
                  string peers = string.Join(", ", warning.Accounts
                     .Where(a => !ReferenceEquals(a, account))
                     .Select(a => $"{a.Service.ServiceName}/{a.Label}"));
                  Warnings.Add(new WarningRowViewModel(
                     Strings.Label_WarnDuplicatedPassword,
                     account.Service.ServiceName,
                     account.Label,
                     peers,
                     account));
               }

               continue;
            }

            if (warning.Accounts is null)
            {
               continue;
            }

            foreach (IAccount account in warning.Accounts)
            {
               string kind = warning.WarningType switch
               {
                  WarningType.PasswordLeakedWarning => Strings.Label_WarnPasswordLeak,
                  WarningType.PasswordUpdateReminderWarning => Strings.Label_RemindPasswordUpdate,
                  WarningType.ActivityReviewWarning => Strings.Label_NeedsReview,
                  _ => warning.WarningType.ToString(),
               };

               Warnings.Add(new WarningRowViewModel(
                  kind,
                  account.Service.ServiceName,
                  account.Label,
                  account.Identifiers.FirstOrDefault() ?? string.Empty,
                  account));
            }
         }

         OnPropertyChanged(nameof(Title));
      }

      private async Task _goToAccountAsync(WarningRowViewModel? row)
      {
         if (row?.TargetAccount is null)
         {
            return;
         }

         ServicesViewModel.RequestSelectAccount(row.TargetAccount);
         await AppServices.Navigation.GoToServicesAsync().ConfigureAwait(true);
      }
   }
}
