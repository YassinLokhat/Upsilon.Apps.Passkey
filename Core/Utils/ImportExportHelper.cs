using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// JSON and tab-separated (TSV) import/export. Files are plaintext by design;
   /// CSV cells are JSON-encoded so commas and quotes in notes survive.
   /// </summary>
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
         => JsonSerializer.Deserialize<T>(json, _options) ?? throw new NullValueException();

      private static readonly JsonSerializerOptions _options = new() { Converters = { new JsonStringEnumConverter(), new ProtectedSecretJsonConverter() }, WriteIndented = true, };

      public static string ImportCSV(this IDatabase database, string importContent)
      {
         List<Service> services = [];

         try
         {
            string[] csvLines = [.. importContent.Split('\n').Select(x => x.Replace("\r", "", StringComparison.Ordinal)).Where(x => !string.IsNullOrWhiteSpace(x))];

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

            if (headersIndexes.Values.Any(x => x == -1))
            {
               return $"the CSV headers should be : {string.Join(", ", headersIndexes.Keys.Select(x => $"'{x}'"))}";
            }

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
         catch (Exception ex)
            when (ex is IndexOutOfRangeException)
         {
            return "the CSV data format is incorrect";
         }

         return services.Count == 0 ? "there is no data to import" : _importServices(database, services);
      }

      public static string ImportJson(this IDatabase database, string importContent)
      {
         Data data;

         try
         {
            data = _jsonDeserializeAs<Data>(importContent);
         }
         catch (JsonException)
         {
            return "import file deserialization failed";
         }

         return _importData(database, data);
      }

      private static string _importData(IDatabase database, Data data)
      {
         string error = string.Empty;

         if (data.Settings is not null)
         {
            error = _importSettings(database, data.Settings);
         }

         if (string.IsNullOrEmpty(error)
            && data.Services is not null)
         {
            error = _importServices(database, data.Services);
         }

         return error;
      }

      private static string _importSettings(IDatabase database, Settings settings)
      {
         if (database.User is null)
         {
            return string.Empty;
         }

         settings.User = (User)database.User;
         database.User.Settings = settings;

         return string.Empty;
      }

      private static string _importServices(IDatabase database, List<Service> services)
      {
         if (database.User is null
            || services.Count == 0)
         {
            return string.Empty;
         }

         Service? s0 = services.FirstOrDefault(x => database.User.Services.Any(y => y.ServiceName == x.ServiceName));
         if (s0 is not null)
         {
            return $"service '{s0.ServiceName}' already exists";
         }

         s0 = services.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.ServiceName));
         if (s0 is not null)
         {
            return $"service name cannot be blank";
         }

         foreach (Service s in services)
         {
            IService service = database.User.AddService(s.ServiceName);
            service.Url = (!string.IsNullOrWhiteSpace(s.Url) && Uri.IsWellFormedUriString(s.Url, UriKind.RelativeOrAbsolute))
               ? new Uri(s.Url) : null;
            service.Notes = s.Notes;

            foreach (Account a in s.Accounts)
            {
               IAccount account = ((Service)service).AddAccount(a.Label, a.Identifiers, a.Password, a.Passwords);
               account.Notes = a.Notes;
               account.Options = a.Options;
               account.PasswordUpdateReminderDelay = a.PasswordUpdateReminderDelay;
            }
         }

         return string.Empty;
      }

      public static string ExportCSV(this Database database, string filePath)
      {
         if (database.User is null)
         {
            return string.Empty;
         }

         StringBuilder sb = new(string.Join("\t", Enum.GetNames<Headers>()) + "\n");

         foreach (Service service in database.User.Services)
         {
            string serviceLine = $"{_jsonSerialize(service.ServiceName.Trim())}\t" +
               $"{_jsonSerialize(service.Url.Trim())}\t" +
               $"{_jsonSerialize(service.Notes.Trim())}\t";

            foreach (Account account in service.Accounts)
            {
               string identifiers = string.Join("|", account.Identifiers.Where(x => !string.IsNullOrWhiteSpace(x)));

               _ = sb.Append(serviceLine);
               _ = sb.Append(CultureInfo.InvariantCulture, $"{_jsonSerialize(account.Label.Trim())}\t" +
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
         if (database.User is null)
         {
            return string.Empty;
         }

         Data data = new()
         {
            Settings = database.User.Settings.CloneWith(database.SerializationCenter),
            Services = [.. database.User.Services],
         };

         File.WriteAllText(filePath, _jsonSerialize(data));

         return string.Empty;
      }
   }

   internal class Data
   {
      public Settings? Settings { get; set; }
      public List<Service>? Services { get; set; }
   }
}
