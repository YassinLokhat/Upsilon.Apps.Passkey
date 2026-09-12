using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Filter helpers and a session keep-alive. <see cref="Shake"/> only exists to
   /// touch <see cref="IItem.ItemId"/>, which resets the inactivity timer via
   /// <c>Database.Get</c> — WPF selection changes would otherwise not count as activity.
   /// </summary>
   internal static class IItemHelper
   {
      /// <summary>
      /// Counts as user activity (resets auto-logout) without changing data.
      /// </summary>
      public static void Shake(this IUser user)
      {
         _ = user.ItemId;
      }

      /// <param name="identifierTypeFilter">
      /// Restricts matching accounts by identifier kind; use <see cref="IdentifierViewModel.AllIdentifierType"/> for any type.
      /// </param>
      public static bool MeetsFilterConditions(
         this IService service,
         string serviceFilter,
         string identifierFilter,
         string globalTextFilter,
         bool changedItemsOnly,
         IdentifierType identifierTypeFilter)
      {
         serviceFilter = serviceFilter.Trim();
         identifierFilter = identifierFilter.Trim();
         globalTextFilter = globalTextFilter.Trim();

         string serviceId = service.ItemId.Trim();
         string serviceName = service.ServiceName.Trim();
         string url = service.Url?.OriginalString.Trim() ?? string.Empty;
         string notes = service.Notes.Trim();

         bool globalTextFilterSearch = serviceId.Equals(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || serviceName.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || url.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || notes.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || service.Accounts.Any(x => x.MeetsFilterConditions(string.Empty, globalTextFilter, changedItemsOnly, identifierTypeFilter));

         bool serviceFilterSearch = string.IsNullOrWhiteSpace(serviceFilter)
            || serviceName.Contains(serviceFilter, StringComparison.OrdinalIgnoreCase);

         bool identifierFilterSearch = service.Accounts.Any(x => x.MeetsFilterConditions(identifierFilter, globalTextFilter, changedItemsOnly, identifierTypeFilter));

         // No identifier/type constraint: keep previous "match all accounts" behavior for the identifier axis.
         if (string.IsNullOrWhiteSpace(identifierFilter)
            && identifierTypeFilter == IdentifierViewModel.AllIdentifierType)
         {
            identifierFilterSearch = true;
         }

         bool serviceAndIdentifierFilterSearch = serviceFilterSearch && identifierFilterSearch;

         bool changedItemsOnlySearch = !changedItemsOnly || service.HasChanged();

         bool filterSearch = !string.IsNullOrWhiteSpace(globalTextFilter)
            ? globalTextFilterSearch
            : serviceAndIdentifierFilterSearch;

         return filterSearch && changedItemsOnlySearch;
      }

      /// <param name="identifierTypeFilter">
      /// Restricts which identifiers participate in the match; use <see cref="IdentifierViewModel.AllIdentifierType"/> for any type.
      /// When a concrete type is set and the account has no identifiers of that type, the account is excluded.
      /// </param>
      public static bool MeetsFilterConditions(
         this IAccount account,
         string identifierFilter,
         string globalTextFilter,
         bool changedItemsOnly,
         IdentifierType identifierTypeFilter)
      {
         identifierFilter = identifierFilter.Trim();
         globalTextFilter = globalTextFilter.Trim();

         bool typeIsAll = identifierTypeFilter == IdentifierViewModel.AllIdentifierType;

         IEnumerable<IIdentifier> typedIdentifiers = typeIsAll
            ? account.Identifiers
            : account.Identifiers.Where(x => x.Type == identifierTypeFilter);

         if (!typeIsAll && !typedIdentifiers.Any())
         {
            return false;
         }

         string accountId = account.ItemId.Trim();
         string label = account.Label.Trim();
         string notes = account.Notes.Trim();
         string identifiers = string.Join("\n", typedIdentifiers.Select(x => x.Value.Trim()));

         bool globalTextFilterSearch = accountId.Equals(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || identifiers.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || label.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || notes.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase);

         bool identifierFilterSearch = string.IsNullOrWhiteSpace(identifierFilter)
               || identifiers.Contains(identifierFilter, StringComparison.OrdinalIgnoreCase)
               || (typeIsAll && label.Contains(identifierFilter, StringComparison.OrdinalIgnoreCase));

         bool changedItemsOnlySearch = !changedItemsOnly || account.HasChanged();

         bool filterSearch = !string.IsNullOrWhiteSpace(globalTextFilter)
            ? globalTextFilterSearch
            : identifierFilterSearch;

         return filterSearch && changedItemsOnlySearch;
      }
   }
}
