using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Adapters
{
   /// <summary>MAUI dialogs via the current page. File pickers use ESSentials.</summary>
   internal sealed class MauiDialogService : IDialogService
   {
      private readonly Dictionary<Type, Func<object, Page>> _views = [];

      private static Page? _page
      {
         get
         {
            IReadOnlyList<Window>? windows = Application.Current?.Windows;
            if (windows is { Count: > 0 })
            {
               return windows[0].Page ?? Shell.Current;
            }

            return Shell.Current;
         }
      }

      public async Task<DialogResult> ConfirmAsync(
         string text,
         string title,
         DialogButtons buttons = DialogButtons.YesNo,
         DialogIcon icon = DialogIcon.Question,
         CancellationToken cancellationToken = default)
      {
         _ = icon;
         cancellationToken.ThrowIfCancellationRequested();
         Page page = _page ?? throw new InvalidOperationException("No active page for dialogs.");

         return buttons switch
         {
            DialogButtons.OK => await _okAsync(page, text, title).ConfigureAwait(true),
            DialogButtons.OKCancel => await _okCancelAsync(page, text, title).ConfigureAwait(true),
            DialogButtons.YesNoCancel => await _yesNoCancelAsync(page, text, title).ConfigureAwait(true),
            _ => await _yesNoAsync(page, text, title).ConfigureAwait(true),
         };
      }

      public async Task InfoAsync(string text, string title, CancellationToken cancellationToken = default)
      {
         cancellationToken.ThrowIfCancellationRequested();
         Page page = _page ?? throw new InvalidOperationException("No active page for dialogs.");
         await page.DisplayAlertAsync(title, text, "OK").ConfigureAwait(true);
      }

      public Task WarnAsync(string text, string title, CancellationToken cancellationToken = default)
         => InfoAsync(text, title, cancellationToken);

      public Task ErrorAsync(string text, string title, CancellationToken cancellationToken = default)
         => InfoAsync(text, title, cancellationToken);

      public async Task<string?> PickBrowseFolderAsync(string title, string defaultPath, CancellationToken cancellationToken = default)
      {
         _ = title;
         _ = defaultPath;
         cancellationToken.ThrowIfCancellationRequested();
#if WINDOWS
         // FolderPicker is WinRT; for v1 fall back to asking the user to pick any file under the folder.
         FileResult? probe = await FilePicker.Default.PickAsync().ConfigureAwait(true);
         return probe is null ? null : Path.GetDirectoryName(probe.FullPath);
#else
         await InfoAsync(
            "On Android, pick a vault document or tree via Open vault (SAF). Folder browse is Windows-oriented.",
            "Folder picker",
            cancellationToken).ConfigureAwait(true);
         return null;
#endif
      }

      public async Task<string?> PickOpenFileAsync(string filter, string title, CancellationToken cancellationToken = default)
      {
         _ = filter;
         cancellationToken.ThrowIfCancellationRequested();
         FileResult? file = await FilePicker.Default.PickAsync(new PickOptions
         {
            PickerTitle = title,
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
               { DevicePlatform.WinUI, [".pku"] },
               { DevicePlatform.Android, ["*/*"] },
            }),
         }).ConfigureAwait(true);
         return file?.FullPath;
      }

      public async Task<string?> PickSaveFileAsync(
         string filter,
         string title,
         string? defaultFileName = null,
         string? initialDirectory = null,
         CancellationToken cancellationToken = default)
      {
         _ = filter;
         _ = title;
         _ = defaultFileName;
         _ = initialDirectory;
         cancellationToken.ThrowIfCancellationRequested();
         // MAUI has no cross-platform save-file picker; hosts use platform APIs in later parity work.
         await InfoAsync("Save-as is not yet available in the MAUI host.", "Save file", cancellationToken)
            .ConfigureAwait(true);
         return null;
      }

      /// <summary>
      /// Maps a request type to the modal page that serves it. The page must
      /// implement <see cref="IResultPage{TResult}"/>.
      /// </summary>
      public void Register<TRequest, TResult>(Func<TRequest, Page> factory)
         where TRequest : IViewRequest<TResult>
      {
         ArgumentNullException.ThrowIfNull(factory);
         _views[typeof(TRequest)] = request => factory((TRequest)request);
      }

      public async Task<TResult?> ShowAsync<TResult>(IViewRequest<TResult> request, CancellationToken cancellationToken = default)
      {
         ArgumentNullException.ThrowIfNull(request);
         cancellationToken.ThrowIfCancellationRequested();

         if (!_views.TryGetValue(request.GetType(), out Func<object, Page>? factory))
         {
            await InfoAsync("This view is not available in the MAUI client yet.", AppServices.Info.Title, cancellationToken)
               .ConfigureAwait(true);
            return default;
         }

         Page page = factory(request);
         if (page is not IResultPage<TResult> resultPage)
         {
            throw new InvalidOperationException($"{page.GetType().Name} does not return {typeof(TResult).Name}.");
         }

         INavigation navigation = (_page ?? throw new InvalidOperationException("No active page for dialogs.")).Navigation;
         await navigation.PushModalAsync(page).ConfigureAwait(true);
         TResult? result = await resultPage.Completion.ConfigureAwait(true);

         if (navigation.ModalStack.Contains(page))
         {
            _ = await navigation.PopModalAsync().ConfigureAwait(true);
         }

         return result;
      }

      private static async Task<DialogResult> _okAsync(Page page, string text, string title)
      {
         await page.DisplayAlertAsync(title, text, "OK").ConfigureAwait(true);
         return DialogResult.OK;
      }

      private static async Task<DialogResult> _okCancelAsync(Page page, string text, string title)
      {
         bool ok = await page.DisplayAlertAsync(title, text, "OK", "Cancel").ConfigureAwait(true);
         return ok ? DialogResult.OK : DialogResult.Cancel;
      }

      private static async Task<DialogResult> _yesNoAsync(Page page, string text, string title)
      {
         bool yes = await page.DisplayAlertAsync(title, text, "Yes", "No").ConfigureAwait(true);
         return yes ? DialogResult.Yes : DialogResult.No;
      }

      private static async Task<DialogResult> _yesNoCancelAsync(Page page, string text, string title)
      {
         string? answer = await page.DisplayActionSheetAsync(title + "\n" + text, "Cancel", null, "Yes", "No")
            .ConfigureAwait(true);
         return answer switch
         {
            "Yes" => DialogResult.Yes,
            "No" => DialogResult.No,
            _ => DialogResult.Cancel,
         };
      }
   }
}
