using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels
{
   /// <summary>One row of the read-only session overview.</summary>
   internal sealed record ServiceSummary(string ServiceName, int AccountCount);

   /// <summary>
   /// Read-only overview of an unlocked vault. Placeholder until the services /
   /// accounts pages reach parity with the WPF services window.
   /// </summary>
   internal sealed class SessionViewModel : ObservableObject
   {
      private readonly IDatabase _database;

      public SessionViewModel(IDatabase database)
      {
         ArgumentNullException.ThrowIfNull(database);
         _database = database;
         Username = database.User?.Username ?? string.Empty;
         Services = database.User is { } user
            ? [.. user.Services
               .OrderBy(s => s.ServiceName, StringComparer.CurrentCultureIgnoreCase)
               .Select(s => new ServiceSummary(s.ServiceName, s.Accounts.Count()))]
            : [];
      }

      public string Username { get; }

      public IReadOnlyList<ServiceSummary> Services { get; }

      public string Summary => Strings.Format(nameof(Strings.Session_Summary), Services.Count, Services.Sum(s => s.AccountCount));

      /// <summary>Closes the session; the login ViewModel handles the write-back and navigation.</summary>
      public void Logout() => _database.Close();
   }
}
