using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class AccountPasswordsAlertViewModel : INotifyPropertyChanged, ILanguageAware
   {
      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property so WPF can refresh Title on language change.")]
      public string Title => Strings.Format(nameof(Strings.Title_AccountPasswordsAlerts), AppInfo.Title);

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
            if (field != value)
            {
               field = value;
               _onPropertyChanged(nameof(ReadableAlertKind));
               RefreshFilters();
            }
         }
      } = EnumHelper.AccountPasswordFilterAll;

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

      public ObservableCollection<AccountPasswordAlertViewModel> Alerts { get; set; } = [];

      public ICommand ClearFiltersCommand { get; }

      public event PropertyChangedEventHandler? PropertyChanged;

      private void _onPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public AccountPasswordsAlertViewModel()
      {
         ClearFiltersCommand = new RelayCommand(ClearFilters);
         RefreshFilters();
      }

      public void OnLanguageChanged()
      {
         _onPropertyChanged(nameof(Title));
         _onPropertyChanged(nameof(ReadableAlertKind));
         RefreshFilters();
      }

      public void ClearFilters()
      {
         Kind = EnumHelper.AccountPasswordFilterAll;
         Text = string.Empty;
      }

      public void RefreshFilters()
      {
         Alerts.Clear();

         AccountPasswordAlertViewModel[] warnings = [.. AppServices.Session.Alerts
            .GetNotifiedAlerts()
            .OfType<IAccountsAlert>()
            .Where(x => _matchesKindFilter(x.Kind, Kind))
            .SelectMany(x => x.Accounts.Select(y => new AccountPasswordAlertViewModel(y, x.Kind)))
            .Where(x => x.MeetsConditions(Kind, Text))];

         foreach (AccountPasswordAlertViewModel warning in warnings)
         {
            Alerts.Add(warning);
         }
      }

      private static bool _matchesKindFilter(string warningKind, string filterKind)
      {
         if (EnumHelper.IsAccountPasswordFilterAll(filterKind))
         {
            return EnumHelper.MatchesAccountPasswordKindFilter(warningKind, filterKind);
         }

         return string.Equals(warningKind, filterKind, StringComparison.Ordinal);
      }
   }
}
