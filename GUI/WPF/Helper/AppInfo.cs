using System.Reflection;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Read-only metadata about the running assembly. Centralises the title shown
   /// in window headers so it is no longer scattered across view-models.
   /// </summary>
   internal static class AppInfo
   {
      private static readonly Lazy<string> _title = new(_buildTitle);

      public static string Title => _title.Value;

      private static string _buildTitle()
      {
         AssemblyName name = Assembly.GetExecutingAssembly().GetName();
         string version = name.Version?.ToString(3) ?? "0.0.0";
         return $"{name.Name} v{version}";
      }
   }
}
