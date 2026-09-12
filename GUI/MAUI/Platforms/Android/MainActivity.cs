using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   // Android requires a public Activity type for the launcher entry point.
   [SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Android launcher Activity must remain public for the OS host.")]
   [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
   public class MainActivity : MauiAppCompatActivity
   {
   }
}
