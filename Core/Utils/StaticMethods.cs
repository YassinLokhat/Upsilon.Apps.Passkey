using System.Text.RegularExpressions;

namespace Upsilon.Apps.PassKey.Core.Utils
{
   internal static class StaticMethods
   {
      public static string ToSentenceCase(this string str) => Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {char.ToLower(m.Value[1])}");
   }
}
