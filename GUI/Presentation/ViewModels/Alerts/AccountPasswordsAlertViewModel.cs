using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts
{
   internal sealed class AccountPasswordsAlertViewModel : ObservableObject, ILanguageRefreshable
   {
      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = Strings.Format(nameof(Strings.Title_AccountPasswordsAlerts), AppServices.Info.Title);

      public string ReadableAlertKind
      {
         get => EnumHelper.ToReadableAlertKind(Kind);
         set => Kind = EnumHelper.AccountPasswordKindFromReadableString(value);
      }

      public string Kind
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(ReadableAlertKind));
               RefreshFilters();
            }
         }
      } = EnumHelper.AccountPasswordFilterAll;

      public string Text
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               RefreshFilters();
            }
         }
      } = "";

      public ObservableCollection<AccountPasswordAlertViewModel> Alerts { get; set; } = [];

      /// <summary>Localized kind filter labels; <see cref="ReadableAlertKind"/> is one of them.</summary>
      public IReadOnlyList<string> KindOptions
      {
         get;
         private set => SetProperty(ref field, value);
      } = _kindOptions();

      public ICommand ClearFiltersCommand { get; }

      public AccountPasswordsAlertViewModel()
      {
         ClearFiltersCommand = new RelayCommand(ClearFilters);
         RefreshFilters();
      }

      public void OnLanguageChanged()
      {
         Title = Strings.Format(nameof(Strings.Title_AccountPasswordsAlerts), AppServices.Info.Title);
         KindOptions = _kindOptions();
         OnPropertyChanged(nameof(ReadableAlertKind));
         RefreshFilters();
      }

      private static string[] _kindOptions()
         =>
         [
            EnumHelper.ToReadableAlertKind(EnumHelper.AccountPasswordFilterAll),
            EnumHelper.ToReadableAlertKind(AlertKinds.PasswordLeaked),
            EnumHelper.ToReadableAlertKind(AlertKinds.PasswordUpdateReminder),
            EnumHelper.ToReadableAlertKind(AlertKinds.WeakAccountPassword),
            EnumHelper.ToReadableAlertKind(AlertKinds.PasskeyReusedAsAccountPassword),
         ];

      public void ClearFilters()
      {
         Kind = EnumHelper.AccountPasswordFilterAll;
         Text = string.Empty;
      }

      public void RefreshFilters()
      {
         Alerts.Clear();

         AccountPasswordAlertViewModel[] alerts = [.. AppServices.Session.Alerts
            .GetNotifiedAlerts()
            .OfType<IAccountsAlert>()
            .Where(x => _matchesKindFilter(x.Kind, Kind))
            .SelectMany(x => x.Accounts.Select(y => new AccountPasswordAlertViewModel(y, x.Kind)))
            .Where(x => x.MeetsConditions(Kind, Text))];

         foreach (AccountPasswordAlertViewModel alert in alerts)
         {
            Alerts.Add(alert);
         }
      }

      private static bool _matchesKindFilter(string alertKind, string filterKind)
      {
         return EnumHelper.IsAccountPasswordFilterAll(filterKind)
            ? EnumHelper.MatchesAccountPasswordKindFilter(alertKind, filterKind)
            : string.Equals(alertKind, filterKind, StringComparison.Ordinal);
      }
   }
}
