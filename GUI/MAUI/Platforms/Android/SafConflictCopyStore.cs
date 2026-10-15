#if ANDROID
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Android
{
   /// <summary>Conflict copies left by <see cref="SafVaultSource"/> in no-backup storage.</summary>
   internal sealed class SafConflictCopyStore : IConflictCopyStore
   {
      public IReadOnlyList<ConflictCopy> List()
         => [.. SafVaultSource.EnumerateConflictCopies().Select(f => new ConflictCopy(f.FullName, f.Name, f.LastWriteTimeUtc))];

      public Stream OpenRead(ConflictCopy copy)
      {
         ArgumentNullException.ThrowIfNull(copy);
         return new FileStream(_checked(copy), FileMode.Open, FileAccess.Read, FileShare.Read);
      }

      public void Delete(ConflictCopy copy)
      {
         ArgumentNullException.ThrowIfNull(copy);
         File.Delete(_checked(copy));
      }

      private static string _checked(ConflictCopy copy)
         => SafVaultSource.EnumerateConflictCopies().Any(f => string.Equals(f.FullName, copy.Id, StringComparison.Ordinal))
            ? copy.Id
            : throw new FileNotFoundException("Unknown conflict copy.");
   }
}
#endif
