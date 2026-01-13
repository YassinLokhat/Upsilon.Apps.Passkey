using System.Text.RegularExpressions;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   public static class StaticMethods
   {
      public static string ToSentenceCase(this string str) => Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {char.ToLower(m.Value[1])}");

      public static string SerializeWith<T>(this T obj, ISerializationCenter serializationCenter) where T : notnull
         => serializationCenter.Serialize(obj);

      public static T DeserializeTo<T>(this string serializedString, ISerializationCenter serializationCenter) where T : notnull
         => serializationCenter.Deserialize<T>(serializedString);

      public static T CloneWith<T>(this T source, ISerializationCenter serializationCenter) where T : notnull
      {
         return source.SerializeWith(serializationCenter).DeserializeTo<T>(serializationCenter);
      }

      public static bool AreDifferent(this ISerializationCenter serializationCenter, object object1, object object2)
      {
         return object1.SerializeWith(serializationCenter) != object2.SerializeWith(serializationCenter);
      }
   }
}
