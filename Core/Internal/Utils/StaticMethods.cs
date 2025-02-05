using System.Text.RegularExpressions;

namespace Upsilon.Apps.PassKey.Core.Internal.Utils
{
   internal static class StaticMethods
   {
      public static string ToSentenceCase(this string str) => Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {char.ToLower(m.Value[1])}");

      public static bool ContainsFlag<T>(this T value, T lookingForFlag) where T : Enum
      {
         if (!typeof(T).IsEnum)
         {
            throw new ArgumentException("T must be an enumerated type");
         }

         int intValue = (int)(object)value;
         int intLookingForFlag = (int)(object)lookingForFlag;
         return (intValue & intLookingForFlag) == intLookingForFlag;
      }
   }
}
