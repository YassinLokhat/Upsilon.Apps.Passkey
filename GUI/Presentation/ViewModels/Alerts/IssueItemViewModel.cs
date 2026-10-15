using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts
{
   /// <summary>One titled issue row in an <see cref="Views.IssuesAlertView"/>.</summary>
   internal sealed class IssueItemViewModel(string title, string description)
   {
      public string Title { get; } = title;
      public string Description { get; } = description;
   }
}
