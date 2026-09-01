namespace Upsilon.Apps.Passkey.GUI.MAUI.Services
{
   internal interface INavigationService
   {
      Task GoToLoginAsync();

      Task GoToServicesAsync();

      Task GoToAppSettingsAsync();

      Task GoToUserSettingsAsync();

      Task GoToPasswordGeneratorAsync();

      Task GoToActivitiesAsync();

      Task GoToWarningsAsync();

      Task GoToQrCodeAsync(string content);

      Task GoToInsertIdentifierAsync(string initialText);

      Task GoBackAsync();
   }
}
