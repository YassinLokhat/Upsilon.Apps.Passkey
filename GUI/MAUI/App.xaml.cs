using Microsoft.Extensions.DependencyInjection;

namespace MAUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new MainPage());
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"CRASH MAUI: {exception?.Message}");
            };

            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            window.Title = "Upsilon.Apps.Passkey.GUI.MAUI";

            window.Created += (s, e) =>
            {
#if WINDOWS
                // Code Windows fonctionnel
                var nativeWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (nativeWindow != null)
                {
                    var handle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                    var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
                    
                    if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                    {
                        presenter.Maximize(); 
                    }
                }
#elif MACCATALYST
                // Astuce Mac Catalyst : On récupère la scène de la fenêtre
                var uiWindow = window.Handler?.PlatformView as UIKit.UIWindow;
                var windowScene = uiWindow?.WindowScene;
                if (windowScene != null && windowScene.SizeRestrictions != null)
                {
                    // On prend les dimensions maximales de l'écran principal de l'utilisateur
                    var screenSize = UIKit.UIScreen.MainScreen.Bounds.Size;
                    
                    // On force temporairement la taille minimale à la taille de l'écran pour l'étirer au max
                    windowScene.SizeRestrictions.MinimumSize = screenSize;
                    windowScene.SizeRestrictions.MaximumSize = screenSize;

                    // On redonne ensuite la liberté à l'utilisateur de redimensionner s'il le veut
                    Dispatcher.Dispatch(() =>
                    {
                        windowScene.SizeRestrictions.MinimumSize = new CoreGraphics.CGSize(500, 600);
                        windowScene.SizeRestrictions.MaximumSize = new CoreGraphics.CGSize(double.PositiveInfinity, double.PositiveInfinity);
                    });
                }
#endif
            };

            return window;
        }
    }
}