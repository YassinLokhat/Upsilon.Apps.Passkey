using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Runtime;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   // Android requires a public Application subclass registered via [Application].
   [SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Android Application entry point must remain public for the OS host.")]
   [Application]
   public class MainApplication : MauiApplication
   {
      public MainApplication(IntPtr handle, JniHandleOwnership ownership)
         : base(handle, ownership)
      {
      }

      protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
   }
}
