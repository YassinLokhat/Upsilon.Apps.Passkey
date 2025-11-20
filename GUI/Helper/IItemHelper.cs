using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.Helper
{
   public static class IItemHelper
   {
      public static bool MeetsFilterConditions(this IService service, string serviceFilter, string identifiantFilter, string textFilter)
      {
         return service.Accounts.Any(x => x.MeetsFilterConditions(identifiantFilter, textFilter))
         || (_matchServiceFilter(service, serviceFilter.ToLower())
            && _matchIdentifiantFilter(service, identifiantFilter.ToLower())
            && _matchTextFilter(service, textFilter.ToLower()));
      }

      private static bool _matchServiceFilter(IService service, string serviceFilter)
      {
         if (string.IsNullOrWhiteSpace(serviceFilter)) return true;

         string serviceId = service.ItemId.ToLower();
         string serviceName = service.ServiceName.ToLower();

         return serviceId.StartsWith(serviceFilter)
            || serviceName.Contains(serviceFilter);
      }

      private static bool _matchIdentifiantFilter(IService service, string identifiantFilter)
      {
         if (string.IsNullOrWhiteSpace(identifiantFilter)) return true;

         string serviceId = service.ItemId.ToLower();
         string serviceName = service.ServiceName.ToLower();

         return serviceId.StartsWith(identifiantFilter)
            || serviceName.Contains(identifiantFilter);
      }

      private static bool _matchTextFilter(IService service, string textFilter)
      {
         if (string.IsNullOrWhiteSpace(textFilter)) return true;

         string serviceId = service.ItemId.ToLower();
         string serviceName = service.ServiceName.ToLower();
         string url = service.Url.ToLower();
         string note = service.Notes.ToLower();

         return serviceId.Contains(textFilter)
            || serviceName.Contains(textFilter)
            || url.Contains(textFilter)
            || note.Contains(textFilter);
      }

      public static bool MeetsFilterConditions(this IAccount account, string identifiantFilter, string textFilter)
         => _matchTextFilter(account, identifiantFilter.ToLower())
            && _matchTextFilter(account, textFilter.ToLower());

      private static bool _matchTextFilter(IAccount account, string textFilter)
      {
         if (string.IsNullOrWhiteSpace(textFilter)) return true;

         string accountId = account.ItemId.ToLower();
         string label = account.Label.ToLower();
         string[] identifiants = [.. account.Identifiants.Select(x => x.ToLower())];

         return accountId.StartsWith(textFilter)
            || label.Contains(textFilter)
            || identifiants.Any(x => x.Contains(textFilter));
      }
   }
}
