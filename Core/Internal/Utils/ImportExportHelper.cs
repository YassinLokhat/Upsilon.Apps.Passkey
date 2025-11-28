using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Upsilon.Apps.Passkey.Core.Internal.Models;
using Upsilon.Apps.Passkey.Core.Public.Enums;
using Upsilon.Apps.Passkey.Core.Public.Interfaces;
using Windows.System;

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
         Identifiers,
         Password,
         AccountNotes,
         AccountOptions,
         PasswordUpdateReminderDelay,
      }

      private static string _jsonSerialize<T>(T obj)
         => JsonSerializer.Serialize(obj, _options);

      private static T _jsonDeserializeAs<T>(string json)
         => JsonSerializer.Deserialize<T>(json, _options) ?? throw new NullReferenceException();

      private static readonly JsonSerializerOptions _options = new() { Converters = { new JsonStringEnumConverter() }, WriteIndented = true, };

      public static string ImportCSV(this IDatabase database, string importContent)
      {
         List<Service> services = [];

         try
         {
            string[] csvLines = [.. importContent.Split('\n').Select(x => x.Replace("\r", "")).Where(x => !string.IsNullOrWhiteSpace(x))];

            string[] headers = csvLines[0].Split("\t");

            Dictionary<Headers, int> headersIndexes = [];

            headersIndexes[Headers.ServiceName] = headers.IndexOf(Headers.ServiceName.ToString());
            headersIndexes[Headers.ServiceUrl] = headers.IndexOf(Headers.ServiceUrl.ToString());
            headersIndexes[Headers.ServiceNotes] = headers.IndexOf(Headers.ServiceNotes.ToString());
            headersIndexes[Headers.AccountLabel] = headers.IndexOf(Headers.AccountLabel.ToString());
            headersIndexes[Headers.Identifiers] = headers.IndexOf(Headers.Identifiers.ToString());
            headersIndexes[Headers.Password] = headers.IndexOf(Headers.Password.ToString());
            headersIndexes[Headers.AccountNotes] = headers.IndexOf(Headers.AccountNotes.ToString());
            headersIndexes[Headers.AccountOptions] = headers.IndexOf(Headers.AccountOptions.ToString());
            headersIndexes[Headers.PasswordUpdateReminderDelay] = headers.IndexOf(Headers.PasswordUpdateReminderDelay.ToString());

            if (headersIndexes.Values.Any(x => x == -1)) return $"the CSV headers should be : {string.Join(", ", headersIndexes.Keys.Select(x => $"'{x}'"))}";

            Service? service = null;

            for (int i = 1; i < csvLines.Length; i++)
            {
               string csvLine = csvLines[i];
               string[] csvColumns = csvLine.Split('\t');
               string serviceName = _jsonDeserializeAs<string>(csvColumns[headersIndexes[Headers.ServiceName]]);
               string serviceUrl = _jsonDeserializeAs<string>(csvColumns[headersIndexes[Headers.ServiceUrl]]);
               string serviceNotes = _jsonDeserializeAs<string>(csvColumns[headersIndexes[Headers.ServiceNotes]]);
               string accountLabel = _jsonDeserializeAs<string>(csvColumns[headersIndexes[Headers.AccountLabel]]);
               string identifiers = _jsonDeserializeAs<string>(csvColumns[headersIndexes[Headers.Identifiers]]);
               string password = _jsonDeserializeAs<string>(csvColumns[headersIndexes[Headers.Password]]);
               string accountNotes = _jsonDeserializeAs<string>(csvColumns[headersIndexes[Headers.AccountNotes]]);
               AccountOption accountOptions = _jsonDeserializeAs<AccountOption>(csvColumns[headersIndexes[Headers.AccountOptions]]);
               int passwordUpdateReminderDelay = _jsonDeserializeAs<int>(csvColumns[headersIndexes[Headers.PasswordUpdateReminderDelay]]);

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
                  Identifiers = [.. identifiers.Split('|').Select(x => x.Trim())],
                  Password = password,
                  Notes = accountNotes,
                  Options = accountOptions,
                  PasswordUpdateReminderDelay = passwordUpdateReminderDelay
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
         Service[] services;

         try
         {
            services = _jsonDeserializeAs<Service[]>(importContent);
         }
         catch
         {
            return "import file deserialization failed";
         }

         return _importServices(database, services);
      }

      private static string _importServices(IDatabase database, IEnumerable<Service> services)
      {
         if (database.User is null) return string.Empty;

         if (!services.Any()) return "there is no data to import";

         Service? s0 = services.FirstOrDefault(x => database.User.Services.Any(y => y.ServiceName == x.ServiceName));
         if (s0 is not null)
         {
            return $"service '{s0.ServiceName}' already exists";
         }

         s0 = services.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.ServiceName) || x.Accounts.Any(y => y.Identifiers.Any(z => string.IsNullOrWhiteSpace(z))));
         if (s0 is not null)
         {
            return $"service name or account identifier cannot be blank";
         }

         foreach (Service s in services)
         {
            IService service = database.User.AddService(s.ServiceName);
            service.Url = s.Url;
            service.Notes = s.Notes;

            foreach (Account a in s.Accounts)
            {
               IAccount account = service.AddAccount(a.Label, a.Identifiers, a.Password);
               account.Notes = a.Notes;
               account.Options = a.Options;
               account.PasswordUpdateReminderDelay = a.PasswordUpdateReminderDelay;
            }
         }

         return string.Empty;
      }

      public static string ExportCSV(this Database database, string filePath)
      {
         if (database.User is null) return string.Empty;

         StringBuilder sb = new(string.Join("\t", Enum.GetNames<Headers>()) + "\n");

         foreach (Service service in database.User.Services)
         {
            string serviceLine = $"{_jsonSerialize(service.ServiceName.Trim())}\t" +
               $"{_jsonSerialize(service.Url.Trim())}\t" +
               $"{_jsonSerialize(service.Notes.Trim())}\t";

            foreach (Account account in service.Accounts)
            {
               string identifiers = string.Join("|", account.Identifiers.Where(x => !string.IsNullOrWhiteSpace(x)));

               sb.Append(serviceLine);
               sb.Append($"{_jsonSerialize(account.Label.Trim())}\t" +
                  $"{_jsonSerialize(identifiers)}\t" +
                  $"{_jsonSerialize(account.Password.Trim())}\t" +
                  $"{_jsonSerialize(account.Notes.Trim())}\t" +
                  $"{_jsonSerialize(account.Options)}\t" +
                  $"{_jsonSerialize(account.PasswordUpdateReminderDelay)}\n");
            }
         }

         File.WriteAllText(filePath, sb.ToString());

         return string.Empty;
      }

      public static string ExportJson(this Database database, string filePath)
      {
         if (database.User is null) return string.Empty;

         File.WriteAllText(filePath, _jsonSerialize(database.User.Services));

         return string.Empty;
      }
   }
}
