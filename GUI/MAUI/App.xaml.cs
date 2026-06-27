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
                
                var uiWindow = window.Handler?.PlatformView as UIKit.UIWindow;
                var windowScene = uiWindow?.WindowScene;
                if (windowScene != null && windowScene.SizeRestrictions != null)
                {               
                    var screenSize = UIKit.UIScreen.MainScreen.Bounds.Size;             
                    
                    windowScene.SizeRestrictions.MinimumSize = screenSize;
                    windowScene.SizeRestrictions.MaximumSize = screenSize;

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