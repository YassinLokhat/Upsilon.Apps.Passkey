using System.Text;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// CSV/TSV row splitting. Export writes tab-separated JSON cells; import
   /// also accepts comma-separated rows (e.g. Excel CSV).
   /// </summary>
   internal static class CSVHelper
   {
      /// <summary>
      /// Splits <paramref name="line"/> on commas and tabs that are neither
      /// backslash-escaped nor inside a quoted cell, so JSON-encoded fields
      /// that contain <c>,</c> or <c>\t</c> stay intact.
      /// </summary>
      public static string[] SplitTabOrCommaDelimited(string line)
      {
         ArgumentNullException.ThrowIfNull(line);

         List<string> fields = [];
         StringBuilder current = new();
         bool inQuotes = false;
         bool escaped = false;

         foreach (char c in line)
         {
            if (escaped)
            {
               _ = current.Append(c);
               escaped = false;
               continue;
            }

            if (c == '\\')
            {
               _ = current.Append(c);
               escaped = true;
               continue;
            }

            if (c == '"')
            {
               inQuotes = !inQuotes;
               _ = current.Append(c);
               continue;
            }

            if (!inQuotes && c is ',' or '\t')
            {
               fields.Add(current.ToString());
               _ = current.Clear();
               continue;
            }

            _ = current.Append(c);
         }

         fields.Add(current.ToString());
         return [.. fields];
      }
   }
}
