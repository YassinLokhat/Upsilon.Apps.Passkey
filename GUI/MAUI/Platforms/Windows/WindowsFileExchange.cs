#if WINDOWS
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Windows
{
   /// <summary>Import/export through the WinRT pickers; the save picker owns the overwrite prompt.</summary>
   internal sealed class WindowsFileExchange : IFileExchange
   {
      public async Task<ExchangeFile?> OpenReadAsync(string title, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default)
      {
         ArgumentNullException.ThrowIfNull(extensions);
         FileOpenPicker picker = new() { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
         foreach (string extension in extensions)
         {
            picker.FileTypeFilter.Add(extension);
         }

         AttachToMainWindow(picker);
         StorageFile? file = await picker.PickSingleFileAsync();
         cancellationToken.ThrowIfCancellationRequested();
         if (file is null)
         {
            return null;
         }

         Stream stream = await file.OpenStreamForReadAsync().ConfigureAwait(true);
         return new ExchangeFile(stream, file.FileType, file.Name);
      }

      public async Task<ExchangeFile?> OpenWriteAsync(string title, string suggestedFileName, string extension, CancellationToken cancellationToken = default)
      {
         FileSavePicker picker = new()
         {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedFileName),
         };
         picker.FileTypeChoices.Add(extension.TrimStart('.').ToUpperInvariant(), [extension]);

         AttachToMainWindow(picker);
         StorageFile? file = await picker.PickSaveFileAsync();
         cancellationToken.ThrowIfCancellationRequested();
         if (file is null)
         {
            return null;
         }

         Stream stream = await file.OpenStreamForWriteAsync().ConfigureAwait(true);
         stream.SetLength(0);
         return new ExchangeFile(stream, extension, file.Name);
      }

      /// <summary>Unpackaged WinUI pickers need the owner window handle.</summary>
      internal static void AttachToMainWindow(object picker)
      {
         if (Application.Current?.Windows is { Count: > 0 } windows && windows[0].Handler?.PlatformView is Microsoft.UI.Xaml.Window window)
         {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
         }
      }
   }
}
#endif
