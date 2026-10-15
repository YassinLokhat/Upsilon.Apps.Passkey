#if ANDROID
using System.Runtime.Versioning;
using Android.Content;
using Android.OS;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using AndroidApp = Android.App.Application;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Android
{
   /// <summary>
   /// Android clipboard with EXTRA_IS_SENSITIVE when API allows.
   /// History scrub has no equivalent — RemoveAllOccurrence is a documented no-op.
   /// <para>
   /// Android 10+ hides the clipboard from an app without focus but still lets it
   /// write. A secret copied before switching apps is therefore cleared at the
   /// deadline (or at session end) unless the clipboard visibly holds other text.
   /// </para>
   /// </summary>
   internal sealed class AndroidClipboardService : IClipboardService, IDisposable
   {
      private readonly Lock _gate = new();
      private string? _tracked;
      private CancellationTokenSource? _clearCts;
      private bool _disposed;

      public event EventHandler<ClipboardSecretCopiedEventArgs>? SecretCopied;

      public void SetText(string text, TimeSpan? autoClearAfter = null)
      {
         if (string.IsNullOrEmpty(text))
         {
            return;
         }

         Context? context = AndroidApp.Context;
         ClipboardManager? cm = context?.GetSystemService(Context.ClipboardService) as ClipboardManager;
         if (cm is null)
         {
            Clipboard.Default.SetTextAsync(text).GetAwaiter().GetResult();
         }
         else
         {
            ClipData? clip = ClipData.NewPlainText("passkey", text);
            if (clip is null)
            {
               Clipboard.Default.SetTextAsync(text).GetAwaiter().GetResult();
            }
            else
            {
               if (OperatingSystem.IsAndroidVersionAtLeast(33) && clip.Description is { } desc)
               {
                  _markSensitive(desc);
               }

               cm.PrimaryClip = clip;
            }
         }

         if (autoClearAfter is { } delay && delay > TimeSpan.Zero)
         {
            SecretCopied?.Invoke(this, new ClipboardSecretCopiedEventArgs(text));
            lock (_gate)
            {
               _tracked = text;
               _clearCts?.Cancel();
               _clearCts = new CancellationTokenSource();
               CancellationToken token = _clearCts.Token;
               _ = Task.Run(async () =>
               {
                  try
                  {
                     await Task.Delay(delay, token).ConfigureAwait(false);
                     ClearIfStillOwned();
                  }
                  catch (System.OperationCanceledException)
                  {
                     // Replaced.
                  }
               }, token);
            }
         }
      }

      public void SetText(string text, int autoClearAfter)
         => SetText(text, autoClearAfter > 0 ? TimeSpan.FromSeconds(autoClearAfter) : null);

      public Task<int> RemoveAllOccurrenceAsync(IEnumerable<string> removeList, CancellationToken cancellationToken = default)
      {
         // No clipboard-history scrub API on Android.
         ArgumentNullException.ThrowIfNull(removeList);
         return Task.FromResult(0);
      }

      public void ClearIfStillOwned()
         => UiThread.Send(_clearIfStillOwnedOnUi);

      private void _clearIfStillOwnedOnUi()
      {
         lock (_gate)
         {
            string? tracked = _tracked;
            _tracked = null;
            if (tracked is null)
            {
               return;
            }

            try
            {
               Context? context = AndroidApp.Context;
               if (context?.GetSystemService(Context.ClipboardService) is not ClipboardManager cm)
               {
                  return;
               }

               // Null without focus: the clip may still be ours.
               if (cm.PrimaryClip is { ItemCount: > 0 } clip
                   && !string.Equals(clip.GetItemAt(0)?.Text, tracked, StringComparison.Ordinal))
               {
                  return;
               }

               if (OperatingSystem.IsAndroidVersionAtLeast(33))
               {
                  cm.ClearPrimaryClip();
               }
               else
               {
                  // ClearPrimaryClip is unreliable on some API 29-32 devices.
                  cm.PrimaryClip = ClipData.NewPlainText(string.Empty, string.Empty);
               }
            }
            catch (Exception ex) when (ex is InvalidOperationException or Java.Lang.Exception)
            {
               AppServices.Log.Warn($"Clipboard clear failed: {ErrorText.ForLog(ex)}");
            }
         }
      }

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         lock (_gate)
         {
            _clearCts?.Cancel();
            _clearCts?.Dispose();
            _clearCts = null;
            _tracked = null;
         }
      }

      [SupportedOSPlatform("android33.0")]
      private static void _markSensitive(ClipDescription desc)
      {
         PersistableBundle? existing = desc.Extras;
         if (existing is not null)
         {
            existing.PutBoolean(ClipDescription.ExtraIsSensitive, true);
            desc.Extras = existing;
            return;
         }

         // Owned by ClipDescription after assignment — do not Dispose.
         PersistableBundle extras = new();
         extras.PutBoolean(ClipDescription.ExtraIsSensitive, true);
         desc.Extras = extras;
      }
   }
}
#endif
