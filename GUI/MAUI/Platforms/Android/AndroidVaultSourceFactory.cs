#if ANDROID
using Android.Content;
using Android.Database;
using Android.Provider;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using AndroidApp = Android.App.Application;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Android
{
   /// <summary>
   /// Opens path-based vaults when the path is a real file; otherwise expects a
   /// prior SAF URI registered via <see cref="RegisterSafUri"/>.
   /// </summary>
   internal sealed class AndroidVaultSourceFactory : IVaultSourceFactory
   {
      private static readonly Lock _gate = new();
      private static readonly Dictionary<string, (global::Android.Net.Uri Uri, string DisplayName)> _safUris = [];

      /// <summary>
      /// Register a SAF document URI (with persistable permission already taken).
      /// Kept for the process lifetime so the same vault can be reopened after a reset.
      /// </summary>
      internal static void RegisterSafUri(global::Android.Net.Uri uri, string displayName)
      {
         ArgumentNullException.ThrowIfNull(uri);
         lock (_gate)
         {
            _safUris[uri.ToString()!] = (uri, displayName);
         }
      }

      /// <summary>Drops a deleted or renamed document and its persisted grant.</summary>
      internal static void Forget(global::Android.Net.Uri uri)
      {
         ArgumentNullException.ThrowIfNull(uri);
         lock (_gate)
         {
            _ = _safUris.Remove(uri.ToString()!);
         }

         try
         {
            AndroidApp.Context.ContentResolver?.ReleasePersistableUriPermission(
               uri,
               ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
         }
         catch (Java.Lang.SecurityException)
         {
            // No persisted grant for this URI (already released or never taken).
         }
      }

      internal static string? QueryDisplayName(global::Android.Net.Uri uri)
      {
         try
         {
            using ICursor? cursor = AndroidApp.Context.ContentResolver?.Query(
               uri, [IOpenableColumns.DisplayName], null, null, null);
            return cursor is not null && cursor.MoveToFirst() ? cursor.GetString(0) : null;
         }
         catch (Java.Lang.Exception ex)
         {
            AppServices.Log.Warn($"SAF display name query failed: {ErrorText.ForLog(ex)}");
            return null;
         }
      }

      public Task<IVaultSource> OpenPathAsync(string path, CancellationToken cancellationToken = default)
      {
         cancellationToken.ThrowIfCancellationRequested();

         // Real filesystem path (app-private working dirs / tests).
         if (!string.IsNullOrWhiteSpace(path)
             && !path.StartsWith("content:", StringComparison.OrdinalIgnoreCase)
             && File.Exists(path))
         {
            return Task.FromResult<IVaultSource>(new PathVaultSource(path));
         }

         lock (_gate)
         {
            if (_safUris.TryGetValue(path, out (global::Android.Net.Uri Uri, string DisplayName) registered))
            {
               return Task.FromResult<IVaultSource>(SafVaultSource.Create(registered.Uri, registered.DisplayName));
            }
         }

         throw new FileNotFoundException("No vault path or SAF URI registered.", path);
      }

      public Task<IVaultSource> CreatePathAsync(string path, CancellationToken cancellationToken = default)
      {
         cancellationToken.ThrowIfCancellationRequested();
         string full = Path.GetFullPath(path);
         string? dir = Path.GetDirectoryName(full);
         if (!string.IsNullOrEmpty(dir))
         {
            _ = Directory.CreateDirectory(dir);
         }

         return Task.FromResult<IVaultSource>(new PathVaultSource(full));
      }

      public async Task<IVaultSource?> CreateInteractiveAsync(string suggestedFileName, CancellationToken cancellationToken = default)
      {
         (global::Android.Net.Uri Uri, string Name)? created = await MainActivity.CreateVaultDocumentAsync(suggestedFileName).ConfigureAwait(true);
         if (created is not { } document)
         {
            return null;
         }

         // Some providers let the user pick an existing file: never build a
         // new vault over existing bytes.
         if (!_isEmpty(document.Uri))
         {
            Forget(document.Uri);
            return null;
         }

         string name = QueryDisplayName(document.Uri) ?? document.Name;
         RegisterSafUri(document.Uri, name);
         return SafVaultSource.CreateNew(document.Uri, name);
      }

      /// <summary>Take persistable read/write permission on a document URI.</summary>
      internal static void TakePersistablePermission(global::Android.Net.Uri uri, bool write)
      {
         ContentResolver? resolver = AndroidApp.Context.ContentResolver;
         if (resolver is null)
         {
            return;
         }

         ActivityFlags flags = ActivityFlags.GrantReadUriPermission;
         if (write)
         {
            flags |= ActivityFlags.GrantWriteUriPermission;
         }

         try
         {
            resolver.TakePersistableUriPermission(uri, flags);
         }
         catch (Java.Lang.SecurityException)
         {
            // Provider may not support persistable grants; open still works for the session.
         }
      }

      private static bool _isEmpty(global::Android.Net.Uri uri)
      {
         using Stream? input = AndroidApp.Context.ContentResolver?.OpenInputStream(uri);
         return input is not null && input.ReadByte() < 0;
      }
   }
}
#endif
