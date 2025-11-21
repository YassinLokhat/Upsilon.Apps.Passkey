using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.Helper
{
   public static class IItemHelper
   {
      public static bool MeetsFilterConditions(this IService service, string serviceFilter, string identifiantFilter, string globalTextFilter)
      {
         serviceFilter = serviceFilter.ToLower().Trim();
         identifiantFilter = identifiantFilter.ToLower().Trim();
         globalTextFilter = globalTextFilter.ToLower().Trim();

         string serviceId = service.ItemId.Replace(service.User.ItemId, string.Empty).ToLower().Trim();
         string serviceName = service.ServiceName.ToLower().Trim();
         string url = service.Url.ToLower().Trim();
         string notes = service.Notes.ToLower().Trim();

         return !string.IsNullOrWhiteSpace(globalTextFilter)
            ? serviceId == globalTextFilter
               || serviceName.Contains(globalTextFilter)
               || url.Contains(globalTextFilter)
               || notes.Contains(globalTextFilter)
               || service.Accounts.Any(x => x.MeetsFilterConditions(string.Empty, globalTextFilter))
            : (string.IsNullOrWhiteSpace(serviceFilter)
                  || (!string.IsNullOrWhiteSpace(serviceFilter) && serviceName.Contains(serviceFilter)))
               && (string.IsNullOrWhiteSpace(identifiantFilter)
                  || service.Accounts.Any(x => x.MeetsFilterConditions(identifiantFilter, globalTextFilter)));
      }

      public static bool MeetsFilterConditions(this IAccount account, string identifiantFilter, string globalTextFilter)
      {
         identifiantFilter = identifiantFilter.ToLower().Trim();
         globalTextFilter = globalTextFilter.ToLower().Trim();

         string accountId = account.ItemId.Replace(account.Service.ItemId, string.Empty).ToLower().Trim();
         string label = account.Label.ToLower().Trim();
         string notes = account.Notes.ToLower().Trim();
         string identifiants = string.Join("\n", account.Identifiants.Select(x => x.ToLower().Trim()));

         return !string.IsNullOrWhiteSpace(globalTextFilter)
            ? accountId == globalTextFilter
               || identifiants.Contains(globalTextFilter)
               || label.ToLower().Contains(globalTextFilter)
               || notes.ToLower().Contains(globalTextFilter)
            : string.IsNullOrWhiteSpace(identifiantFilter)
               || identifiants.Contains(identifiantFilter)
               || label.Contains(identifiantFilter);
      }
   }
}
