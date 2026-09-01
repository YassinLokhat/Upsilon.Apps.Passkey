using Upsilon.Apps.Passkey.GUI.MAUI.Views;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   public partial class AppShell : Shell
   {
      public AppShell()
      {
         InitializeComponent();
         Routing.RegisterRoute(nameof(CreateUserPage), typeof(CreateUserPage));
         Routing.RegisterRoute(nameof(AppSettingsPage), typeof(AppSettingsPage));
         Routing.RegisterRoute(nameof(UserSettingsPage), typeof(UserSettingsPage));
         Routing.RegisterRoute(nameof(PasswordGeneratorPage), typeof(PasswordGeneratorPage));
         Routing.RegisterRoute(nameof(ActivitiesPage), typeof(ActivitiesPage));
         Routing.RegisterRoute(nameof(WarningsPage), typeof(WarningsPage));
         Routing.RegisterRoute(nameof(QrCodePage), typeof(QrCodePage));
         Routing.RegisterRoute(nameof(InsertIdentifierPage), typeof(InsertIdentifierPage));
      }
   }
}
