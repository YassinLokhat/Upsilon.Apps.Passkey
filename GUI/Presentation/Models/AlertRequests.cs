namespace Upsilon.Apps.Passkey.GUI.Presentation.Models
{
   /// <summary>
   /// Opens the activity log; <paramref name="NeedsReview"/> preselects the review filter and
   /// a non-null <paramref name="SearchCriteria"/> (e.g. an item id) resets the other filters.
   /// </summary>
   internal sealed record ActivitiesRequest(bool NeedsReview, string? SearchCriteria = null) : IViewRequest<bool>;

   /// <summary>Opens account-password alerts filtered on <paramref name="Kind"/> (empty = all).</summary>
   internal sealed record AccountPasswordsAlertRequest(string Kind) : IViewRequest<bool>;

   internal sealed record DuplicatedPasswordsAlertRequest : IViewRequest<bool>;

   internal sealed record PasskeyQualityAlertRequest : IViewRequest<bool>;

   internal sealed record SecuritySettingsAlertRequest : IViewRequest<bool>;
}
