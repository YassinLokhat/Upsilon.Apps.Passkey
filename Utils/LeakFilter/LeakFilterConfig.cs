using System.Security;

namespace Upsilon.Apps.Passkey.Utils.LeakFilter
{
   /// <summary>
   /// Application-level leak-filter preferences (not stored in the vault).
   /// </summary>
   public sealed class LeakFilterConfig
   {
      public static LeakFilterConfig Default { get; } = new();

      /// <summary>
      /// When <see langword="true"/> and the <c>.pkbf</c> exists, use it after HIBP/XON fail.
      /// Disabling never deletes the on-disk filter.
      /// </summary>
      public bool Enabled { get; set; } = true;

      /// <summary>
      /// Optional absolute override path for the <c>.pkbf</c>; empty means
      /// <see cref="LeakFilterPaths.FilterFilePath"/> under the current root.
      /// </summary>
      public string FilterPath { get; set; } = Path.GetFullPath(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "pwned-sha1.pkbf"));

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
            or ArgumentNullException
            or InvalidDataException
            or NotSupportedException
            or IOException
            or SecurityException
            or DirectoryNotFoundException
            or UnauthorizedAccessException
            or PathTooLongException
            or ArgumentOutOfRangeException)
         {
            System.Diagnostics.Trace.TraceWarning($"Offline leak filter could not be opened: {ex}");
            return null;
         }
      }

      /// <summary>
      /// Deletes the resolved <c>.pkbf</c> when present. Does not change <see cref="LeakFilterConfig.Enabled"/>.
      /// </summary>
      public bool TryDeleteFilterFile()
      {
         if (!File.Exists(FilterPath))
         {
            return false;
         }

         File.Delete(FilterPath);
         return true;
      }
   }
}
