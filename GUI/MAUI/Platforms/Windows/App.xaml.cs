using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Upsilon.Apps.Passkey.GUI.MAUI.WinUI
{
   /// <summary>
   /// Provides application-specific behavior to supplement the default Application class.
   /// </summary>
   // WinUI's generated Program entry instantiates this type reflectively; it must stay public.
   [SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "WinUI application entry point must remain public for the host.")]
   public partial class App : MauiWinUIApplication
   {
      /// <summary>
      /// Initializes the singleton application object.  This is the first line of authored code
      /// executed, and as such is the logical equivalent of main() or WinMain().
      /// </summary>
      public App()
      {
         this.InitializeComponent();
      }

      protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
   }

}
