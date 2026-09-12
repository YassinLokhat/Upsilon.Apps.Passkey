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
#if IOS || MACCATALYST
         static () => new AppDelegate(),
#endif
      ];
   }
}
