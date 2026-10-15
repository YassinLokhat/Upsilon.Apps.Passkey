using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Android;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   // Android requires a public Activity type for the launcher entry point.
   [SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Android launcher Activity must remain public for the OS host.")]
   [Activity(
      Theme = "@style/Maui.SplashTheme",
      MainLauncher = true,
      LaunchMode = LaunchMode.SingleTask,
      ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
   public class MainActivity : MauiAppCompatActivity
   {
      private const int OPEN_DOCUMENT_REQUEST = 0x504B; // 'PK'

      private static readonly string[] _openDocumentMimeTypes = ["application/octet-stream", "*/*"];

      private const int CREATE_DOCUMENT_REQUEST = 0x504C;

      private static TaskCompletionSource<(global::Android.Net.Uri Uri, string Name)?>? _pickTcs;

      private static TaskCompletionSource<(global::Android.Net.Uri Uri, string Name)?>? _createTcs;

      private const int EXCHANGE_DOCUMENT_REQUEST = 0x504D;

      private static TaskCompletionSource<global::Android.Net.Uri?>? _exchangeTcs;

      protected override void OnCreate(Bundle? savedInstanceState)
      {
         base.OnCreate(savedInstanceState);
         // Keep secrets out of screenshots / recents / screen share.
         Window?.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
         if (OperatingSystem.IsAndroidVersionAtLeast(33))
         {
            SetRecentsScreenshotEnabled(false);
         }

         AndroidVaultSourceFactory.ReleaseStaleGrants();
      }

      /// <summary>ACTION_OPEN_DOCUMENT for a single .pku (persistable read/write).</summary>
      internal static Task<(global::Android.Net.Uri Uri, string Name)?> PickVaultDocumentAsync()
      {
         if (Platform.CurrentActivity is not MainActivity activity)
         {
            return Task.FromResult<(global::Android.Net.Uri, string)?>(null);
         }

         _pickTcs?.TrySetResult(null);
         _pickTcs = new TaskCompletionSource<(global::Android.Net.Uri, string)?>();

         using Intent intent = new(Intent.ActionOpenDocument);
         intent.AddCategory(Intent.CategoryOpenable);
         intent.SetType("*/*");
         intent.PutExtra(Intent.ExtraMimeTypes, _openDocumentMimeTypes);
         intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPersistableUriPermission);

         activity.StartActivityForResult(intent, OPEN_DOCUMENT_REQUEST);
         return _pickTcs.Task;
      }

      /// <summary>ACTION_CREATE_DOCUMENT for a new .pku (persistable read/write).</summary>
      internal static Task<(global::Android.Net.Uri Uri, string Name)?> CreateVaultDocumentAsync(string suggestedFileName)
      {
         if (Platform.CurrentActivity is not MainActivity activity)
         {
            return Task.FromResult<(global::Android.Net.Uri, string)?>(null);
         }

         _createTcs?.TrySetResult(null);
         _createTcs = new TaskCompletionSource<(global::Android.Net.Uri, string)?>();

         using Intent intent = new(Intent.ActionCreateDocument);
         intent.AddCategory(Intent.CategoryOpenable);
         intent.SetType("application/octet-stream");
         intent.PutExtra(Intent.ExtraTitle, suggestedFileName);
         intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPersistableUriPermission);

         activity.StartActivityForResult(intent, CREATE_DOCUMENT_REQUEST);
         return _createTcs.Task;
      }

      /// <summary>
      /// Runs an open/create document intent for a one-shot import or export.
      /// No persistable grant is taken: the URI is only valid for this operation.
      /// </summary>
      internal static Task<global::Android.Net.Uri?> StartExchangeDocumentAsync(Intent intent)
      {
         ArgumentNullException.ThrowIfNull(intent);
         if (Platform.CurrentActivity is not MainActivity activity)
         {
            return Task.FromResult<global::Android.Net.Uri?>(null);
         }

         _exchangeTcs?.TrySetResult(null);
         _exchangeTcs = new TaskCompletionSource<global::Android.Net.Uri?>();
         activity.StartActivityForResult(intent, EXCHANGE_DOCUMENT_REQUEST);
         return _exchangeTcs.Task;
      }

      protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
      {
         base.OnActivityResult(requestCode, resultCode, data);

         if (requestCode == EXCHANGE_DOCUMENT_REQUEST)
         {
            TaskCompletionSource<global::Android.Net.Uri?>? exchange = _exchangeTcs;
            _exchangeTcs = null;
            exchange?.TrySetResult(resultCode == Result.Ok ? data?.Data : null);
            return;
         }

         TaskCompletionSource<(global::Android.Net.Uri Uri, string Name)?>? tcs;
         switch (requestCode)
         {
            case OPEN_DOCUMENT_REQUEST:
               tcs = _pickTcs;
               _pickTcs = null;
               break;
            case CREATE_DOCUMENT_REQUEST:
               tcs = _createTcs;
               _createTcs = null;
               break;
            default:
               return;
         }

         // No pending request (process recreated mid-pick): never take a grant nobody will use.
         if (tcs is null)
         {
            return;
         }

         if (resultCode != Result.Ok || data?.Data is not { } uri)
         {
            tcs.TrySetResult(null);
            return;
         }

         AndroidVaultSourceFactory.TakePersistablePermission(uri, write: true);
         // LastPathSegment is the provider's document id and can contain folder names.
         string name = AndroidVaultSourceFactory.QueryDisplayName(uri) ?? "vault.pku";
         tcs.TrySetResult((uri, name));
      }
   }
}
