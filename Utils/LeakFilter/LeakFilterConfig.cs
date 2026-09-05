using System.Security;

namespace Upsilon.Apps.Passkey.Utils.LeakFilter
{
   /// <summary>
   /// Application-level leak-filter preferences (not stored in the vault).
   /// </summary>
   public sealed class LeakFilterConfig(string filterPath)
   {
      /// <summary>
      /// When <see langword="true"/> and the <c>.pkbf</c> exists, use it after HIBP/XON fail.
      /// Disabling never deletes the on-disk filter.
      /// </summary>
      public bool Enabled { get; set; } = true;

      /// <summary>
      /// When <see langword="true"/>, refresh an existing <c>.pkbf</c> in the background
      /// at startup. Never triggers a first full build (too heavy for automatic use).
      /// </summary>
      public bool AutoUpdateEnabled { get; set; }

      /// <summary>
      /// Absolute path of the <c>.pkbf</c>; defaults to <c>pwned-sha1.pkbf</c> next to the
      /// executable.
      /// </summary>
      public string FilterPath { get; set; } = filterPath;

      /// <summary>
      /// Opens the configured filter when enabled and present; otherwise returns <see langword="null"/>.
      /// </summary>
      public ILocalLeakFilter? TryOpenConfiguredFilter()
      {
         if (!Enabled)
         {
            return null;
         }

         if (!File.Exists(FilterPath))
         {
            return null;
         }

         try
         {
            return HibpBloomFile.Open(FilterPath);
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or InvalidDataException
            or NotSupportedException
            or IOException
            or SecurityException
            or UnauthorizedAccessException)
         {
            System.Diagnostics.Trace.TraceWarning($"Offline leak filter could not be opened: {ex}");
            return null;
         }
      }

      /// <summary>
      /// Deletes the resolved <c>.pkbf</c> when present, along with its range
      /// sidecar. Does not change <see cref="LeakFilterConfig.Enabled"/>.
      /// </summary>
      public bool TryDeleteFilterFile()
      {
         // The sidecar goes first: an orphaned one would be rejected on the next
         // build anyway, but leaving it behind wastes tens of megabytes.
         HibpRangeStateStore.DeleteFor(FilterPath);

         if (!File.Exists(FilterPath))
         {
            return false;
         }

         File.Delete(FilterPath);
         return true;
      }
   }
}
