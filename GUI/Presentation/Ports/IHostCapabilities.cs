namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>
   /// Features that depend on the host platform. ViewModels hide or disable
   /// the matching UI instead of testing the platform directly.
   /// </summary>
   internal interface IHostCapabilities
   {
      /// <summary>Offline <c>.pkbf</c> leak filter (Windows only; too large for mobile).</summary>
      bool SupportsOfflineLeakFilter { get; }

      /// <summary>Vaults are addressed by file-system paths (otherwise by picker / SAF URIs).</summary>
      bool SupportsFileSystemPaths { get; }

      /// <summary>Several top-level windows can be open at once.</summary>
      bool SupportsMultipleWindows { get; }

      /// <summary>Keyboard shortcuts and menu bar are available.</summary>
      bool SupportsKeyboardShortcuts { get; }
   }

   internal sealed record HostCapabilities(
      bool SupportsOfflineLeakFilter,
      bool SupportsFileSystemPaths,
      bool SupportsMultipleWindows,
      bool SupportsKeyboardShortcuts) : IHostCapabilities
   {
      public static HostCapabilities Desktop { get; } = new(true, true, true, true);

      public static HostCapabilities Mobile { get; } = new(false, false, false, false);
   }
}
