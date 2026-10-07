using System.Text;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// Documented size / count ceilings for untrusted local files (.pku ZIP,
   /// import JSON/CSV, activity decrypt). Prevents zip bombs and oversized
   /// allocations from forged inputs (UPK-001).
   /// </summary>
   internal static class ResourceBudgets
   {
      internal const long MaxArchiveBytes = 64L * 1024 * 1024;
      internal const long MaxEntryStoredBytes = 64L * 1024 * 1024;
      internal const long MaxEntryDecodedBytes = 64L * 1024 * 1024;
      internal const long MaxImportFileBytes = 64L * 1024 * 1024;
      internal const int MaxZipEntries = 4;
      internal const int MaxActivityEntries = 100_000;

      internal static readonly HashSet<string> AllowedZipEntryNames = new(StringComparer.Ordinal)
      {
         "header",
         "database",
         "autosave",
         "activity",
      };

      internal static bool IsAllowedZipEntryName(string name)
         => AllowedZipEntryNames.Contains(name);

      /// <summary>
      /// Copies from <paramref name="input"/> to <paramref name="output"/> and
      /// throws <see cref="InvalidDataException"/> if more than
      /// <paramref name="limit"/> bytes are transferred.
      /// </summary>
      internal static void CopyBounded(Stream input, Stream output, long limit)
      {
         byte[] buffer = new byte[81920];
         long total = 0;
         int read;
         while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
         {
            total += read;
            if (total > limit)
            {
               throw new InvalidDataException($"Stream exceeds the maximum of {limit} bytes.");
            }

            output.Write(buffer, 0, read);
         }
      }

      /// <summary>
      /// Reads a stream into a UTF-8 string with a hard byte ceiling.
      /// </summary>
      internal static string ReadUtf8Bounded(Stream input, long limit)
      {
         using MemoryStream buffer = new();
         CopyBounded(input, buffer, limit);
         return Encoding.UTF8.GetString(buffer.ToArray());
      }
   }
}
