namespace Upsilon.Apps.Passkey.GUI.MAUI.Services
{
   internal interface IDialogService
   {
      Task InfoAsync(string text, string title);

      Task WarnAsync(string text, string title);

      Task<bool> ConfirmAsync(string text, string title);

      Task<ConfirmThreeWayResult> ConfirmThreeWayAsync(string text, string title);

      /// <param name="fileTypeHint">
      /// Optional extension hint: <c>pku</c>, <c>json</c>, <c>csv</c>, or <c>*</c> / empty for any.
      /// </param>
      Task<string?> PickOpenFileAsync(string title, string? fileTypeHint = "pku");

      Task<string?> PickSaveFileAsync(string title, string suggestedFileName, string? fileTypeHint = null);

      Task<string?> PickFolderAsync(string title, string? defaultPath = null);
   }
}
