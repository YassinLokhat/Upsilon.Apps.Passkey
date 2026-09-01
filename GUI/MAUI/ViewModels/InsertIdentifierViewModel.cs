using System.Collections.ObjectModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class InsertIdentifierViewModel : ObservableObject
   {
      private readonly string[] _allIdentifiers;

      public InsertIdentifierViewModel(IEnumerable<string> knownIdentifiers, string initialText)
      {
         _allIdentifiers = knownIdentifiers
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

         Identifier = initialText ?? string.Empty;
         Suggestions = [];
         _refreshSuggestions();

         SelectCommand = new RelayCommand(p =>
         {
            if (p is string s)
            {
               Identifier = s;
            }
         });
         ConfirmCommand = new AsyncRelayCommand(_confirmAsync);
         CancelCommand = new AsyncRelayCommand(() => AppServices.Navigation.GoBackAsync());
      }

      public string Title => Strings.Title_InsertIdentifier;

      public string Identifier
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _refreshSuggestions();
            }
         }
      } = string.Empty;

      public ObservableCollection<string> Suggestions { get; }

      public ICommand SelectCommand { get; }
      public ICommand ConfirmCommand { get; }
      public ICommand CancelCommand { get; }

      internal static string? PendingResult { get; private set; }
      internal static string? PendingServiceItemId { get; private set; }
      internal static string? PendingAccountItemId { get; private set; }

      internal static void BeginInsertFor(IAccount account)
      {
         ArgumentNullException.ThrowIfNull(account);
         PendingServiceItemId = account.Service.ItemId;
         PendingAccountItemId = account.ItemId;
         PendingResult = null;
      }

      internal static void ClearPendingResult()
      {
         PendingResult = null;
         PendingServiceItemId = null;
         PendingAccountItemId = null;
      }

      private void _refreshSuggestions()
      {
         Suggestions.Clear();
         string needle = Identifier.Trim();
         IEnumerable<string> query = _allIdentifiers;

         if (!string.IsNullOrEmpty(needle))
         {
            query = _allIdentifiers
               .Where(x => x.StartsWith(needle, StringComparison.OrdinalIgnoreCase))
               .Concat(_allIdentifiers.Where(x =>
                  x.Contains(needle, StringComparison.OrdinalIgnoreCase)
                  && !x.StartsWith(needle, StringComparison.OrdinalIgnoreCase)));
         }

         foreach (string item in query.Take(50))
         {
            Suggestions.Add(item);
         }
      }

      private async Task _confirmAsync()
      {
         string value = Identifier.Trim();
         if (string.IsNullOrEmpty(value))
         {
            return;
         }

         PendingResult = value;
         await AppServices.Navigation.GoBackAsync().ConfigureAwait(true);
      }
   }
}
