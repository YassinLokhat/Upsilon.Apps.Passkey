using CommunityToolkit.Maui;
using MAUI;
using Microsoft.Extensions.Logging;
using Upsilon.Apps.Passkey.Core.Services;
using Upsilon.Apps.Passkey.Core.Utils;

using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;
using Upsilon.Apps.Passkey.Interface.Services;

using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.UI.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // --- Interface & Core Registrations ---
            builder.Services.AddSingleton<ICryptographyCenter, CryptographyCenter>();
            builder.Services.AddTransient<PkuLifecycleManager>();

            // --- UI / MAUI Implementation Registrations ---
            builder.Services.AddSingleton<IBiometricAuthService, BiometricAuthService>();
            builder.Services.AddSingleton<ISecureStorageService, MauiSecureStorageService>();

            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<MainPage>(); 

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}