using Upsilon.Apps.Passkey.GUI.Presentation.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>Portable dialog / file-picker surface for ViewModels.</summary>
   internal interface IDialogService
   {
      Task<DialogResult> ConfirmAsync(string text, string title, DialogButtons buttons = DialogButtons.YesNo, DialogIcon icon = DialogIcon.Question, CancellationToken cancellationToken = default);

      Task InfoAsync(string text, string title, CancellationToken cancellationToken = default);

      Task WarnAsync(string text, string title, CancellationToken cancellationToken = default);

      Task ErrorAsync(string text, string title, CancellationToken cancellationToken = default);

      Task<string?> PickBrowseFolderAsync(string title, string defaultPath, CancellationToken cancellationToken = default);

      Task<string?> PickOpenFileAsync(string filter, string title, CancellationToken cancellationToken = default);

      Task<string?> PickSaveFileAsync(string filter, string title, string? defaultFileName = null, string? initialDirectory = null, CancellationToken cancellationToken = default);

      /// <summary>Show a host-specific view by logical key (e.g. <c>PasswordGenerator</c>).</summary>
      Task ShowViewAsync(string viewKey, object? parameter = null, CancellationToken cancellationToken = default);

      /// <summary>Show a modal host view; returns when closed.</summary>
      Task<object?> ShowModalViewAsync(string viewKey, object? parameter = null, CancellationToken cancellationToken = default);
   }
}
