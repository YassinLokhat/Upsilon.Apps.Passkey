#if WINDOWS
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Windows
{
   /// <summary>WinRT pickers that return file-system paths (folder browse, new vault location).</summary>
   internal static class WindowsPathPickers
   {
      public static async Task<string?> PickFolderAsync()
      {
         FolderPicker picker = new() { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
         picker.FileTypeFilter.Add("*");

         WindowsFileExchange.AttachToMainWindow(picker);
         StorageFolder? folder = await picker.PickSingleFolderAsync();
         return folder?.Path;
      }

      /// <summary>
      /// The save picker creates an empty file for a new name. It is deleted so the
      /// caller creates the target itself and still refuses a file with content.
      /// </summary>
      public static async Task<string?> PickNewFileAsync(string suggestedFileName)
      {
         string extension = Path.GetExtension(suggestedFileName);
         if (extension.Length == 0)
         {
            extension = ".pku";
         }

         FileSavePicker picker = new()
         {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedFileName),
         };
         picker.FileTypeChoices.Add(extension.TrimStart('.').ToUpperInvariant(), [extension]);

         WindowsFileExchange.AttachToMainWindow(picker);
         StorageFile? file = await picker.PickSaveFileAsync();
         if (file is null)
         {
            return null;
         }

         FileInfo target = new(file.Path);
         if (target.Exists && target.Length == 0)
         {
            target.Delete();
         }

         return target.FullName;
      }
   }
}
#endif
