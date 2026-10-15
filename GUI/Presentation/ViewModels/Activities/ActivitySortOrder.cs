namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Activities
{
   /// <summary>
   /// Activity list orders, offered where the host has no sortable columns.
   /// Values index <see cref="UserActivitiesViewModel.SortOptions"/>.
   /// </summary>
   internal enum ActivitySortOrder
   {
      NewestFirst = 0,
      OldestFirst = 1,
      EventType = 2,
      NeedsReviewFirst = 3,
      Message = 4,
   }
}
