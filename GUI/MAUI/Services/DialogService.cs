using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Services
{
   internal sealed class DialogService : IDialogService
   {
      public async Task InfoAsync(string text, string title)
      {
         Page? page = _currentPage();
         if (page is null)
         {
            return;
         }

         await page.DisplayAlertAsync(title, text, Strings.Button_OK).ConfigureAwait(true);
      }

      public async Task WarnAsync(string text, string title)
      {
         Page? page = _currentPage();
         if (page is null)
         {
            return;
         }

         await page.DisplayAlertAsync(title, text, Strings.Button_OK).ConfigureAwait(true);
      }

      public async Task<bool> ConfirmAsync(string text, string title)
      {
         Page? page = _currentPage();
         if (page is null)
         {
            return false;
         }

         return await page.DisplayAlertAsync(title, text, Strings.Button_Yes, Strings.Button_No).ConfigureAwait(true);
      }

      public async Task<ConfirmThreeWayResult> ConfirmThreeWayAsync(string text, string title)
      {
         Page? page = _currentPage();
         if (page is null)
         {
            return ConfirmThreeWayResult.Cancel;
         }

         // MAUI has no native Yes/No/Cancel; use ActionSheet.
         string? choice = await page.DisplayActionSheetAsync(
            title + Environment.NewLine + text,
            Strings.Button_Cancel,
            null,
            Strings.Button_Yes,
            Strings.Button_No).ConfigureAwait(true);

         if (string.Equals(choice, Strings.Button_Yes, StringComparison.Ordinal))
         {
            return ConfirmThreeWayResult.Yes;
         }

         if (string.Equals(choice, Strings.Button_No, StringComparison.Ordinal))
         {
            return ConfirmThreeWayResult.No;
         }

         return ConfirmThreeWayResult.Cancel;
      }

      public async Task<string?> PickOpenFileAsync(string title, string? fileTypeHint = "pku")
      {
         try
         {
            FilePickerFileType fileTypes = _openFileTypes(fileTypeHint);
            FileResult? result = await FilePicker.Default.PickAsync(new PickOptions
            {
               PickerTitle = title,
               FileTypes = fileTypes,
            }).ConfigureAwait(true);

            return result?.FullPath;
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or ArgumentNullException
            or InvalidOperationException
            or NotSupportedException
            or TimeoutException
            or TaskCanceledException
            or OperationCanceledException
            or FileNotFoundException
            or UnauthorizedAccessException)
         {
            Log.Error(ex, "File picker failed");
            return null;
         }
      }

      public async Task<string?> PickSaveFileAsync(string title, string suggestedFileName, string? fileTypeHint = null)
      {
         _ = title;
         _ = fileTypeHint;

#if WINDOWS
         try
         {
            // Prefer letting the user pick a folder, then append the suggested name.
            string? folder = await PickFolderAsync(title, Path.GetDirectoryName(suggestedFileName)).ConfigureAwait(true);
            if (folder is null)
            {
               return null;
            }

            string name = Path.GetFileName(suggestedFileName);
            if (string.IsNullOrEmpty(name))
            {
               name = "export.json";
            }

            return Path.Join(folder, name);
         }
         catch (Exception ex)
            when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
         {
            Log.Error(ex, "Save picker failed");
            return suggestedFileName;
         }
#else
         string directory = AppPaths.DefaultVaultDirectory;
         _ = Directory.CreateDirectory(directory);
         string name = Path.GetFileName(suggestedFileName);
         if (string.IsNullOrEmpty(name))
         {
            name = "vault.pku";
         }

         return Path.Join(directory, name);
#endif
      }

      public async Task<string?> PickFolderAsync(string title, string? defaultPath = null)
      {
         _ = title;

#if WINDOWS
         try
         {
            // Windows App SDK FolderPicker via reflection so we don't hard-require WinRT APIs at compile time on all TFMs.
            Type? pickerType = Type.GetType("Windows.Storage.Pickers.FolderPicker, Microsoft.Windows.SDK.NET")
               ?? Type.GetType("Windows.Storage.Pickers.FolderPicker, Windows");

            if (pickerType is null)
            {
               Log.Warn("FolderPicker type not found; falling back to default vault directory.");
               return Directory.Exists(defaultPath) ? defaultPath : AppPaths.DefaultVaultDirectory;
            }

            dynamic picker = Activator.CreateInstance(pickerType)!;
            picker.SuggestedStartLocation = 0; // PickerLocationId.DocumentsLibrary
            picker.FileTypeFilter.Add("*");

            // Initialize with window handle when possible.
            nint hwnd = _tryGetWindowHandle();
            if (hwnd != 0)
            {
               Type? initType = Type.GetType("WinRT.Interop.InitializeWithWindow, Microsoft.Windows.SDK.NET")
                  ?? Type.GetType("WinRT.Interop.InitializeWithWindow, WinRT.Runtime");
               initType?.GetMethod("Initialize")?.Invoke(null, [picker, hwnd]);
            }

            dynamic? folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
               return null;
            }

            return (string)folder.Path;
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or InvalidOperationException
            or NotSupportedException
            or UnauthorizedAccessException
            or TimeoutException
            or TaskCanceledException
            or OperationCanceledException
            or MissingMethodException
            or TypeLoadException)
         {
            Log.Error(ex, "Folder picker failed");
            return Directory.Exists(defaultPath) ? defaultPath : AppPaths.DefaultVaultDirectory;
         }
#else
         await Task.CompletedTask.ConfigureAwait(true);
         string fallback = Directory.Exists(defaultPath) ? defaultPath! : AppPaths.DefaultVaultDirectory;
         _ = Directory.CreateDirectory(fallback);
         return fallback;
#endif
      }

      private static FilePickerFileType _openFileTypes(string? hint)
      {
         string normalized = (hint ?? "*").Trim().TrimStart('.').ToUpperInvariant();
         return normalized switch
         {
            "PKU" => new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
               { DevicePlatform.WinUI, [".pku"] },
               { DevicePlatform.Android, ["application/octet-stream", "*/*"] },
            }),
            "JSON" => new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
               { DevicePlatform.WinUI, [".json"] },
               { DevicePlatform.Android, ["application/json", "*/*"] },
            }),
            "CSV" => new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
               { DevicePlatform.WinUI, [".csv", ".tsv", ".txt"] },
               { DevicePlatform.Android, ["text/csv", "text/plain", "*/*"] },
            }),
            _ => new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
               { DevicePlatform.WinUI, [".*"] },
               { DevicePlatform.Android, ["*/*"] },
            }),
         };
      }

#if WINDOWS
      private static nint _tryGetWindowHandle()
      {
         try
         {
            IReadOnlyList<Window> windows = Application.Current?.Windows ?? [];
            if (windows.Count == 0)
            {
               return 0;
            }

            object? native = windows[0].Handler?.PlatformView;
            if (native is null)
            {
               return 0;
            }

            // MauiWinUIWindow has WindowHandle
            System.Reflection.PropertyInfo? prop = native.GetType().GetProperty("WindowHandle");
            if (prop?.GetValue(native) is nint handle)
            {
               return handle;
            }

            return 0;
         }
         catch (Exception ex)
            when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
         {
            return 0;
         }
      }
#endif

      private static Page? _currentPage()
      {
         IReadOnlyList<Window> windows = Application.Current?.Windows ?? [];
         return windows.Count > 0 ? windows[0].Page : null;
      }
   }
}
