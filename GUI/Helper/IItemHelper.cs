using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Helper
{
   public static class IItemHelper
   {
      public static void Shake(this IUser user)
      {
         _ = user.ItemId;
      }

      public static bool MeetsFilterConditions(this IService service, string serviceFilter, string identifierFilter, string globalTextFilter, bool changedItemsOnly)
      {
         serviceFilter = serviceFilter.ToLower().Trim();
         identifierFilter = identifierFilter.ToLower().Trim();
         globalTextFilter = globalTextFilter.ToLower().Trim();

         string serviceId = service.ItemId.Replace(service.User.ItemId, string.Empty).ToLower().Trim();
         string serviceName = service.ServiceName.ToLower().Trim();
         string url = service.Url.ToLower().Trim();
         string notes = service.Notes.ToLower().Trim();

         return (!string.IsNullOrWhiteSpace(globalTextFilter)
            ? serviceId == globalTextFilter
               || serviceName.Contains(globalTextFilter)
               || url.Contains(globalTextFilter)
               || notes.Contains(globalTextFilter)
               || service.Accounts.Any(x => x.MeetsFilterConditions(string.Empty, globalTextFilter, changedItemsOnly))
            : (string.IsNullOrWhiteSpace(serviceFilter)
                  || (!string.IsNullOrWhiteSpace(serviceFilter) && serviceName.Contains(serviceFilter)))
               && (string.IsNullOrWhiteSpace(identifierFilter)
                  || service.Accounts.Any(x => x.MeetsFilterConditions(identifierFilter, globalTextFilter, changedItemsOnly))))
            && (!changedItemsOnly || service.HasChanged());
      }

      public static bool MeetsFilterConditions(this IAccount account, string identifierFilter, string globalTextFilter, bool changedItemsOnly)
      {
         identifierFilter = identifierFilter.ToLower().Trim();
         globalTextFilter = globalTextFilter.ToLower().Trim();

         string accountId = account.ItemId.Replace(account.Service.ItemId, string.Empty).ToLower().Trim();
         string label = account.Label.ToLower().Trim();
         string notes = account.Notes.ToLower().Trim();
         string identifiers = string.Join("\n", account.Identifiers.Select(x => x.ToLower().Trim()));

         return (!string.IsNullOrWhiteSpace(globalTextFilter)
            ? accountId == globalTextFilter
               || identifiers.Contains(globalTextFilter)
               || label.ToLower().Contains(globalTextFilter)
               || notes.ToLower().Contains(globalTextFilter)
            : string.IsNullOrWhiteSpace(identifierFilter)
               || identifiers.Contains(identifierFilter)
               || label.Contains(identifierFilter))
            && (!changedItemsOnly || account.HasChanged());
      }
   }
}
