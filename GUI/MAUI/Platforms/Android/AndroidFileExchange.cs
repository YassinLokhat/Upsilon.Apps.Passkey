#if ANDROID
using Android.Content;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using AndroidApp = Android.App.Application;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Android
{
   /// <summary>
   /// Import/export through SAF documents. Streams go straight to the provider so
   /// plaintext exports never touch app storage.
   /// </summary>
   internal sealed class AndroidFileExchange : IFileExchange
   {
      private static readonly string[] _importMimeTypes = ["application/json", "text/csv", "text/comma-separated-values", "text/plain", "application/octet-stream"];

      public async Task<ExchangeFile?> OpenReadAsync(string title, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default)
      {
         using Intent intent = new(Intent.ActionOpenDocument);
         intent.AddCategory(Intent.CategoryOpenable);
         intent.SetType("*/*");
         intent.PutExtra(Intent.ExtraMimeTypes, _importMimeTypes);
         intent.AddFlags(ActivityFlags.GrantReadUriPermission);

         global::Android.Net.Uri? uri = await MainActivity.StartExchangeDocumentAsync(intent).ConfigureAwait(true);
         cancellationToken.ThrowIfCancellationRequested();
         if (uri is null)
         {
            return null;
         }

         string name = AndroidVaultSourceFactory.QueryDisplayName(uri) ?? string.Empty;
         Stream stream = AndroidApp.Context.ContentResolver?.OpenInputStream(uri)
            ?? throw new IOException("The document provider returned no stream.");
         return new ExchangeFile(stream, Path.GetExtension(name), name);
      }

      public async Task<ExchangeFile?> OpenWriteAsync(string title, string suggestedFileName, string extension, CancellationToken cancellationToken = default)
      {
         using Intent intent = new(Intent.ActionCreateDocument);
         intent.AddCategory(Intent.CategoryOpenable);
         intent.SetType(string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase) ? "application/json" : "text/csv");
         intent.PutExtra(Intent.ExtraTitle, suggestedFileName);
         intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

         global::Android.Net.Uri? uri = await MainActivity.StartExchangeDocumentAsync(intent).ConfigureAwait(true);
         cancellationToken.ThrowIfCancellationRequested();
         if (uri is null)
         {
            return null;
         }

         // "wt" truncates when the provider hands back an existing document.
         Stream stream = AndroidApp.Context.ContentResolver?.OpenOutputStream(uri, "wt")
            ?? throw new IOException("The document provider returned no stream.");
         return new ExchangeFile(stream, extension, AndroidVaultSourceFactory.QueryDisplayName(uri) ?? suggestedFileName);
      }
   }
}
#endif
