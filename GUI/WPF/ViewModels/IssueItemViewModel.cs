namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   /// <summary>One titled issue row in an <see cref="Views.IssuesAlertView"/>.</summary>
   internal sealed class IssueItemViewModel(string title, string description)
   {
      public string Title { get; } = title;
      public string Description { get; } = description;
   }
}
