namespace Upsilon.Apps.Passkey.GUI.Presentation.Models
{
   /// <summary>Opens the activity log; <paramref name="NeedsReview"/> preselects the review filter.</summary>
   internal sealed record ActivitiesRequest(bool NeedsReview) : IViewRequest<bool>;

   /// <summary>Opens account-password alerts filtered on <paramref name="Kind"/> (empty = all).</summary>
   internal sealed record AccountPasswordsAlertRequest(string Kind) : IViewRequest<bool>;

   internal sealed record DuplicatedPasswordsAlertRequest : IViewRequest<bool>;

   internal sealed record PasskeyQualityAlertRequest : IViewRequest<bool>;

   internal sealed record SecuritySettingsAlertRequest : IViewRequest<bool>;
}
