namespace Upsilon.Apps.PassKey.Core.Public.Interfaces
{
   /// <summary>
   /// Represent a serialization center.
   /// </summary>
   public interface ISerializationCenter
   {
      /// <summary>
      /// Serialize the given object to a string.
      /// </summary>
      /// <typeparam name="T">The type of the object.</typeparam>
      /// <param name="toSerialize">The object to serialize.</param>
      /// <returns>The serialised string.</returns>
      string Serialize<T>(T toSerialize) where T : notnull;

      /// <summary>
      /// Deserialize the given string to the given type object.
      /// </summary>
      /// <typeparam name="T">The type of the object.</typeparam>
      /// <param name="toDeserialize">The serialised string.</param>
      /// <returns>The deserialized object.</returns>
      T Deserialize<T>(string toDeserialize) where T : notnull;

      /// <summary>
      /// Check if two objects are different or not.
      /// </summary>
      /// <param name="serializationCenter">The Serialization Center.</param>
      /// <param name="object1">The first object.</param>
      /// <param name="object2">The second object.</param>
      /// <returns>True if the two objects are different, False else.</returns>
      public static bool AreDifferent(ISerializationCenter serializationCenter, object object1, object object2)
      {
         return serializationCenter.Serialize(object1) != serializationCenter.Serialize(object2);
      }

      /// <summary>
      /// Clone the given object.
      /// </summary>
      /// <typeparam name="T">The type of the object to clone.</typeparam>
      /// <param name="serializationCenter">The Serialization Center.</param>
      /// <param name="source">The object to clone.</param>
      /// <returns>The clone of the object.</returns>
      public static T Clone<T>(ISerializationCenter serializationCenter, T source) where T : notnull
      {
         return serializationCenter.Deserialize<T>(serializationCenter.Serialize(source));
      }
   }
}
