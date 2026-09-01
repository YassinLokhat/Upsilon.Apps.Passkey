using Upsilon.Apps.Passkey.GUI.MAUI.Views;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Services
{
   internal sealed class NavigationService : INavigationService
   {
      public async Task GoToLoginAsync()
         => await Shell.Current.GoToAsync("//LoginPage").ConfigureAwait(true);

      public async Task GoToServicesAsync()
         => await Shell.Current.GoToAsync("//ServicesPage").ConfigureAwait(true);

      public async Task GoToAppSettingsAsync()
         => await Shell.Current.GoToAsync(nameof(AppSettingsPage)).ConfigureAwait(true);

      public async Task GoToUserSettingsAsync()
         => await Shell.Current.GoToAsync(nameof(UserSettingsPage)).ConfigureAwait(true);

      public async Task GoToPasswordGeneratorAsync()
         => await Shell.Current.GoToAsync(nameof(PasswordGeneratorPage)).ConfigureAwait(true);

      public async Task GoToActivitiesAsync()
         => await Shell.Current.GoToAsync(nameof(ActivitiesPage)).ConfigureAwait(true);

      public async Task GoToWarningsAsync()
         => await Shell.Current.GoToAsync(nameof(WarningsPage)).ConfigureAwait(true);

      public async Task GoToQrCodeAsync(string content)
      {
         await Shell.Current.GoToAsync(
            nameof(QrCodePage),
            new Dictionary<string, object> { ["Content"] = content ?? string.Empty }).ConfigureAwait(true);
      }

      public async Task GoToInsertIdentifierAsync(string initialText)
      {
         await Shell.Current.GoToAsync(
            nameof(InsertIdentifierPage),
            new Dictionary<string, object> { ["Initial"] = initialText ?? string.Empty }).ConfigureAwait(true);
      }

      public async Task GoBackAsync()
      {
         if (Shell.Current.Navigation.NavigationStack.Count > 1)
         {
            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
         }
         else
         {
            await GoToServicesAsync().ConfigureAwait(true);
         }
      }
   }
}
