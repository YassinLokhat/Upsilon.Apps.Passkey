namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   /// <summary>
   /// Object-creation sites for types that MAUI instantiates via DI, XAML, or the
   /// platform host. Present so CA1812 treats them as used; the factories are never
   /// invoked at runtime.
   /// </summary>
   internal static class MauiFrameworkCreatedTypeAnchors
   {
      internal static readonly Func<object>[] Factories =
      [
         static () => new App(),
         static () => new MainPage(),
         static () => new Adapters.MauiDialogService(),
         static () => new Adapters.MauiThemePlatform(),
         static () => new Adapters.MauiUiTimerFactory(),
         static () => new Adapters.MauiClipboardService(),
#if WINDOWS
         static () => new Platforms.Windows.WindowsClipboardService(),
#endif
#if ANDROID
         static () => new Platforms.Android.AndroidClipboardService(),
         static () => new Platforms.Android.AndroidVaultSourceFactory(),
         static () => Platforms.Android.SafVaultSource.Create(
            global::Android.Net.Uri.Parse("content://passkey/unused")!,
            "unused"),
#endif
      ];
   }
}
