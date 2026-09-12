using Microsoft.Extensions.Logging;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   internal static class MauiProgram
   {
      public static MauiApp CreateMauiApp()
      {
         // Touch so CA1812 sees framework-created types as used (factories are never invoked).
         _ = MauiFrameworkCreatedTypeAnchors.Factories.Length;

         var builder = MauiApp.CreateBuilder();
         builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
               fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
               fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
         builder.Logging.AddDebug();
#endif

         return builder.Build();
      }
   }
}
