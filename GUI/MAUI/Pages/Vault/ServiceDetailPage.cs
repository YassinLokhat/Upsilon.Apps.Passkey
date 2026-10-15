using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Vault;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Vault
{
   /// <summary>Phone layout: one service editor pushed on top of <see cref="VaultPage"/>.</summary>
   internal sealed class ServiceDetailPage : ContentPage
   {
      public ServiceDetailPage(UserServicesViewModel vault, ServiceViewModel service)
      {
         ArgumentNullException.ThrowIfNull(vault);
         ArgumentNullException.ThrowIfNull(service);

         BindingContext = service;
         Title = service.ServiceName;
         Content = new ServiceEditorView { BindingContext = service };

         ToolbarItems.AddLocalized(nameof(Strings.Menu_Save), vault.SaveCommand);
         ToolbarItems.AddLocalized(nameof(Strings.Menu_GenerateRandomPassword), vault.GeneratePasswordCommand, ToolbarItemOrder.Secondary);
         ToolbarItems.AddLocalized(nameof(Strings.Vault_DeleteService), vault.DeleteServiceCommand, ToolbarItemOrder.Secondary);
      }

      protected override void OnDisappearing()
      {
         base.OnDisappearing();
         IReadOnlyList<Page> stack = Navigation.NavigationStack;
         if (stack.Count == 0 || stack[^1] != this)
         {
            Content = null;
         }
      }
   }
}
