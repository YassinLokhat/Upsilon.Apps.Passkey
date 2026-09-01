using Upsilon.Apps.Passkey.GUI.MAUI.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Helpers
{
   /// <summary>
   /// Cross-platform hotkey façade. Windows implementation lives under Platforms/Windows.
   /// </summary>
   internal static partial class HotkeyService
   {
#if !WINDOWS
      public static void Register(Func<string?> getIdentifier, Func<string?> getPassword)
      {
         _ = getIdentifier;
         _ = getPassword;
      }

      public static void Unregister()
      {
      }
#endif
   }
}
