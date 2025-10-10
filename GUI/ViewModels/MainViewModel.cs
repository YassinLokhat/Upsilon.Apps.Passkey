namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class MainViewModel
   {
      public static string AppTitle
      {
         get
         {
            var package = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            var packageVersion = package.Version?.ToString(2);

            return $"{package.Name} v{packageVersion}";
         }
      }
   }
}
