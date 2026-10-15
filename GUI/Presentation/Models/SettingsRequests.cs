namespace Upsilon.Apps.Passkey.GUI.Presentation.Models
{
   /// <summary>Opens the user (vault) settings; completes when the view closes.</summary>
   internal sealed record UserSettingsRequest : IViewRequest<bool>;

   /// <summary>Opens the application settings; completes when the view closes.</summary>
   internal sealed record AppSettingsRequest : IViewRequest<bool>;
}
