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

      public static bool MeetsFilterConditions(this IService service, string serviceFilter, string identifierFilter, string globalTextFilter, bool changedItemsOnly)
      {
         serviceFilter = serviceFilter.Trim();
         identifierFilter = identifierFilter.Trim();
         globalTextFilter = globalTextFilter.Trim();

         string serviceId = service.ItemId.Trim();
         string serviceName = service.ServiceName.Trim();
         string url = service.Url?.OriginalString.Trim() ?? string.Empty;
         string notes = service.Notes.Trim();

         return (!string.IsNullOrWhiteSpace(globalTextFilter)
            ? serviceId.Equals(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || serviceName.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || url.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || notes.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || service.Accounts.Any(x => x.MeetsFilterConditions(string.Empty, globalTextFilter, changedItemsOnly))
            : (string.IsNullOrWhiteSpace(serviceFilter)
                  || (!string.IsNullOrWhiteSpace(serviceFilter) && serviceName.Contains(serviceFilter, StringComparison.OrdinalIgnoreCase)))
               && (string.IsNullOrWhiteSpace(identifierFilter)
                  || service.Accounts.Any(x => x.MeetsFilterConditions(identifierFilter, globalTextFilter, changedItemsOnly))))
            && (!changedItemsOnly || service.HasChanged());
      }

      public static bool MeetsFilterConditions(this IAccount account, string identifierFilter, string globalTextFilter, bool changedItemsOnly)
      {
         identifierFilter = identifierFilter.Trim();
         globalTextFilter = globalTextFilter.Trim();

         string accountId = account.ItemId.Trim();
         string label = account.Label.Trim();
         string notes = account.Notes.Trim();
         string identifiers = string.Join("\n", account.Identifiers.Select(x => x.Trim()));

         return (!string.IsNullOrWhiteSpace(globalTextFilter)
            ? accountId.Equals(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || identifiers.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || label.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase)
               || notes.Contains(globalTextFilter, StringComparison.OrdinalIgnoreCase)
            : string.IsNullOrWhiteSpace(identifierFilter)
               || identifiers.Contains(identifierFilter, StringComparison.OrdinalIgnoreCase)
               || label.Contains(identifierFilter, StringComparison.OrdinalIgnoreCase))
            && (!changedItemsOnly || account.HasChanged());
      }
   }
}
