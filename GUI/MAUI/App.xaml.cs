using Microsoft.Extensions.DependencyInjection;

namespace MAUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new MainPage());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

            // On applique la limite UNIQUEMENT si l'application tourne sur Windows ou Mac
            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI ||
                DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst)
            {
                // Limite minimale (Empêche de faire trop petit)
                window.MinimumWidth = 500;
                window.MinimumHeight = 600;

                // Optionnel : Tu peux aussi définir une taille de démarrage par défaut
                window.Width = 600;
                window.Height = 700;
            }

            return window;
        }
    }
}