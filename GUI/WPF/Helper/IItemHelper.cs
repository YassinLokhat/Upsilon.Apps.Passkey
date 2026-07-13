using System.Globalization;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   internal static class IItemHelper
   {
      public static void Shake(this IUser user)
      {
         _ = user.ItemId;
      }

      public static bool MeetsFilterConditions(this IService service, string serviceFilter, string identifierFilter, string globalTextFilter, bool changedItemsOnly)
      {
         serviceFilter = serviceFilter.ToLower(CultureInfo.CurrentCulture).Trim();
         identifierFilter = identifierFilter.ToLower(CultureInfo.CurrentCulture).Trim();
         globalTextFilter = globalTextFilter.ToLower(CultureInfo.CurrentCulture).Trim();

         string serviceId = service.ItemId.Replace(service.User.ItemId, string.Empty, StringComparison.CurrentCulture).ToLower(CultureInfo.CurrentCulture).Trim();
         string serviceName = service.ServiceName.ToLower(CultureInfo.CurrentCulture).Trim();
         string url = service.Url?.OriginalString.ToLower(CultureInfo.CurrentCulture).Trim() ?? string.Empty;
         string notes = service.Notes.ToLower(CultureInfo.CurrentCulture).Trim();

         return (!string.IsNullOrWhiteSpace(globalTextFilter)
            ? serviceId == globalTextFilter
               || serviceName.Contains(globalTextFilter, StringComparison.CurrentCulture)
               || url.Contains(globalTextFilter, StringComparison.CurrentCulture)
               || notes.Contains(globalTextFilter, StringComparison.CurrentCulture)
               || service.Accounts.Any(x => x.MeetsFilterConditions(string.Empty, globalTextFilter, changedItemsOnly))
            : (string.IsNullOrWhiteSpace(serviceFilter)
                  || (!string.IsNullOrWhiteSpace(serviceFilter) && serviceName.Contains(serviceFilter, StringComparison.CurrentCulture)))
               && (string.IsNullOrWhiteSpace(identifierFilter)
                  || service.Accounts.Any(x => x.MeetsFilterConditions(identifierFilter, globalTextFilter, changedItemsOnly))))
            && (!changedItemsOnly || service.HasChanged());
      }

      public static bool MeetsFilterConditions(this IAccount account, string identifierFilter, string globalTextFilter, bool changedItemsOnly)
      {
         identifierFilter = identifierFilter.ToLower(CultureInfo.CurrentCulture).Trim();
         globalTextFilter = globalTextFilter.ToLower(CultureInfo.CurrentCulture).Trim();

         string accountId = account.ItemId.Replace(account.Service.ItemId, string.Empty, StringComparison.CurrentCulture).ToLower(CultureInfo.CurrentCulture).Trim();
         string label = account.Label.ToLower(CultureInfo.CurrentCulture).Trim();
         string notes = account.Notes.ToLower(CultureInfo.CurrentCulture).Trim();
         string identifiers = string.Join("\n", account.Identifiers.Select(x => x.ToLower(CultureInfo.CurrentCulture).Trim()));

         return (!string.IsNullOrWhiteSpace(globalTextFilter)
            ? accountId == globalTextFilter
               || identifiers.Contains(globalTextFilter, StringComparison.CurrentCulture)
               || label.ToLower(CultureInfo.CurrentCulture).Contains(globalTextFilter, StringComparison.CurrentCulture)
               || notes.ToLower(CultureInfo.CurrentCulture).Contains(globalTextFilter, StringComparison.CurrentCulture)
            : string.IsNullOrWhiteSpace(identifierFilter)
               || identifiers.Contains(identifierFilter, StringComparison.CurrentCulture)
               || label.Contains(identifierFilter, StringComparison.CurrentCulture))
            && (!changedItemsOnly || account.HasChanged());
      }
   }
}
