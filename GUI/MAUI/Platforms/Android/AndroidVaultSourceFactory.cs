#if ANDROID
using Android.Content;
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
   }
}
#endif
