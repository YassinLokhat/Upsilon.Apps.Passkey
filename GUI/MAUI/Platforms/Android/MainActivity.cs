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

      protected override void OnCreate(Bundle? savedInstanceState)
      {
         base.OnCreate(savedInstanceState);
         // Keep secrets out of screenshots / recents / screen share.
         Window?.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
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

      protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
      {
         base.OnActivityResult(requestCode, resultCode, data);

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

         if (resultCode != Result.Ok || data?.Data is not { } uri)
         {
            tcs?.TrySetResult(null);
            return;
         }

         AndroidVaultSourceFactory.TakePersistablePermission(uri, write: true);
         string name = uri.LastPathSegment ?? "vault.pku";
         if (name.Contains('/', StringComparison.Ordinal))
         {
            name = name[(name.LastIndexOf('/') + 1)..];
         }

         tcs?.TrySetResult((uri, name));
      }
   }
}
