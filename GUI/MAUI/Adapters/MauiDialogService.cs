using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
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
         await page.DisplayAlertAsync(title, text, Strings.Button_OK).ConfigureAwait(true);
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
         return await Platforms.Windows.WindowsPathPickers.PickFolderAsync().ConfigureAwait(true);
#else
         await InfoAsync(
            Strings.Format(nameof(Strings.Maui_FolderPickerAndroid), Strings.Login_OpenVault),
            Strings.Maui_Title_FolderPicker,
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
         _ = initialDirectory;
         cancellationToken.ThrowIfCancellationRequested();
#if WINDOWS
         return await Platforms.Windows.WindowsPathPickers.PickNewFileAsync(defaultFileName ?? string.Empty).ConfigureAwait(true);
#else
         // Android hosts create documents through IVaultSourceFactory / IFileExchange, never by path.
         _ = defaultFileName;
         await InfoAsync(Strings.Maui_SaveAsUnavailable, Strings.Maui_Title_SaveFile, cancellationToken)
            .ConfigureAwait(true);
         return null;
#endif
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
            await InfoAsync(Strings.Maui_ViewUnavailable, AppServices.Info.Title, cancellationToken)
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
         await page.DisplayAlertAsync(title, text, Strings.Button_OK).ConfigureAwait(true);
         return DialogResult.OK;
      }

      private static async Task<DialogResult> _okCancelAsync(Page page, string text, string title)
      {
         bool ok = await page.DisplayAlertAsync(title, text, Strings.Button_OK, Strings.Button_Cancel).ConfigureAwait(true);
         return ok ? DialogResult.OK : DialogResult.Cancel;
      }

      private static async Task<DialogResult> _yesNoAsync(Page page, string text, string title)
      {
         bool yes = await page.DisplayAlertAsync(title, text, Strings.Button_Yes, Strings.Button_No).ConfigureAwait(true);
         return yes ? DialogResult.Yes : DialogResult.No;
      }

      private static async Task<DialogResult> _yesNoCancelAsync(Page page, string text, string title)
      {
         // The action sheet returns the tapped label; compare against the labels shown.
         string yes = Strings.Button_Yes;
         string no = Strings.Button_No;
         string? answer = await page.DisplayActionSheetAsync(title + "\n" + text, Strings.Button_Cancel, null, yes, no)
            .ConfigureAwait(true);

         return string.Equals(answer, yes, StringComparison.Ordinal) ? DialogResult.Yes
            : string.Equals(answer, no, StringComparison.Ordinal) ? DialogResult.No
            : DialogResult.Cancel;
      }
   }
}
