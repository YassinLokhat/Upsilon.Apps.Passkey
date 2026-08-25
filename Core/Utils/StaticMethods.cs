using System.Text.RegularExpressions;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// Serialization helpers used by autosave (clone-by-JSON, structural equality).
   /// </summary>
   public static class StaticMethods
   {
      /// <summary>
      /// Inserts a space before an internal capital: <c>ItemUpdated</c> → <c>Item updated</c>.
      /// Used for activity messages, not as a locale-aware formatter.
      /// </summary>
      public static string ToSentenceCase(this string str)
         => string.IsNullOrEmpty(str) ? str : str[..1].ToUpperInvariant() + str[1..];

      public static string SerializeWith<T>(this T obj, ISerializationCenter serializationCenter) where T : notnull
      {
         ArgumentNullException.ThrowIfNull(serializationCenter);

         return serializationCenter.Serialize(obj);
      }

      public static T DeserializeTo<T>(this string serializedString, ISerializationCenter serializationCenter) where T : notnull
      {
         ArgumentNullException.ThrowIfNull(serializationCenter);

         return serializationCenter.Deserialize<T>(serializedString);
      }

      /// <summary>
      /// Round-trips <paramref name="source"/> through JSON so nested collections
      /// become a deep copy rather than a shared reference.
      /// </summary>
      public static T CloneWith<T>(this T source, ISerializationCenter serializationCenter) where T : notnull
      {
         return source.SerializeWith(serializationCenter).DeserializeTo<T>(serializationCenter);
      }

      /// <summary>
      /// Structural inequality via serialized form (order-sensitive). Used to
      /// decide whether an edit should create an autosave <c>Change</c>.
      /// </summary>
      public static bool AreDifferent(this ISerializationCenter serializationCenter, object object1, object object2)
      {
         return object1.SerializeWith(serializationCenter) != object2.SerializeWith(serializationCenter);
      }
   }
}
