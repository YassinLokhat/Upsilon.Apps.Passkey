using Upsilon.Apps.Passkey.Core.Internal.Models;
using Upsilon.Apps.Passkey.Core.Public.Interfaces;
using Windows.UI.Composition;

namespace Upsilon.Apps.Passkey.Core.Internal.Utils
{
   internal static class ImportExportHelper
   {
      private enum Headers
      {
         ServiceName,
         ServiceUrl,
         ServiceNotes,
         AccountLabel,
         Identifiants,
         Password,
         AccountNotes,
      }

      public static string ImportCSV(this IDatabase database, string importContent)
      {
         List<IService> services = [];

         try
         {
            string[] csvLines = importContent.Split('\n');

            string[] headers = csvLines[0].Split("\t");

            Dictionary<Headers, int> headersIndexes = [];

            headersIndexes[Headers.ServiceName] = headers.IndexOf(Headers.ServiceName.ToString());
            headersIndexes[Headers.ServiceUrl] = headers.IndexOf(Headers.ServiceUrl.ToString());
            headersIndexes[Headers.ServiceNotes] = headers.IndexOf(Headers.ServiceNotes.ToString());
            headersIndexes[Headers.AccountLabel] = headers.IndexOf(Headers.AccountLabel.ToString());
            headersIndexes[Headers.Identifiants] = headers.IndexOf(Headers.Identifiants.ToString());
            headersIndexes[Headers.Password] = headers.IndexOf(Headers.Password.ToString());
            headersIndexes[Headers.AccountNotes] = headers.IndexOf(Headers.AccountNotes.ToString());

            if (headersIndexes.Values.Any(x => x == -1)) return $"the CSV headers should be : {string.Join(", ", headersIndexes.Keys.Select(x => $"'{x}'"))}";

            Service? service = null;

            for (int i = 1; i < csvLines.Length; i++)
            {
               string csvLine = csvLines[i];
               string[] csvColumns = csvLine.Split('\t');
               string serviceName = database.SerializationCenter.Deserialize<string>(csvColumns[headersIndexes[Headers.ServiceName]]);
               string serviceUrl = database.SerializationCenter.Deserialize<string>(csvColumns[headersIndexes[Headers.ServiceUrl]]);
               string serviceNotes = database.SerializationCenter.Deserialize<string>(csvColumns[headersIndexes[Headers.ServiceNotes]]);
               string accountLabel = database.SerializationCenter.Deserialize<string>(csvColumns[headersIndexes[Headers.AccountLabel]]);
               string identifiants = database.SerializationCenter.Deserialize<string>(csvColumns[headersIndexes[Headers.Identifiants]]);
               string password = database.SerializationCenter.Deserialize<string>(csvColumns[headersIndexes[Headers.Password]]);
               string accountNotes = database.SerializationCenter.Deserialize<string>(csvColumns[headersIndexes[Headers.AccountNotes]]);

               if (service is null
                  || service.ServiceName != serviceName)
               {
                  service = new()
                  {
                     ServiceName = serviceName,
                     Url = serviceUrl,
                     Notes = serviceNotes,
                  };

                  services.Add(service);
               }

               Account account = new()
               {
                  Label = accountLabel,
                  Identifiants = [.. identifiants.Split('|').Where(x => !string.IsNullOrWhiteSpace(x))],
                  Password = password,
                  Notes = accountNotes,
               };

               service.Accounts.Add(account);
            }
         }
         catch
         {
            return "the CSV data format is incorrect";
         }

         return _importServices(database, services);
      }

      public static string ImportJson(this IDatabase database, string importContent)
      {
         IService[] services;

         try
         {
            services = database.SerializationCenter.Deserialize<IService[]>(importContent);
         }
         catch
         {
            return "import file deserialization failed";
         }

         return _importServices(database, services);
      }

      private static string _importServices(IDatabase database, IEnumerable<IService> services)
      {
         if (database.User is null) return string.Empty;

         if (!services.Any()) return "there is no data to import";

         IService? service = services.FirstOrDefault(x => database.User.Services.Any(y => y.ServiceName == x.ServiceName));
         if (service is not null)
         {
            return $"service '{service.ServiceName}' already exists";
         }

         foreach (IService s in services)
         {
            service = database.User.AddService(s.ServiceName);
            service.Url = s.Url;
            service.Notes = s.Notes;

            foreach (IAccount a in service.Accounts)
            {
               IAccount account = service.AddAccount(a.Label, a.Identifiants, a.Password);
               account.Notes = a.Notes;
               account.Options = a.Options;
               account.PasswordUpdateReminderDelay = a.PasswordUpdateReminderDelay;
            }
         }

         return string.Empty;
      }

      public static void Export(this Database database, string filePath)
      {
         throw new NotImplementedException();
      }
   }
}
