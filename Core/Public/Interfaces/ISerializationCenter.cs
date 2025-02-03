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
   }
}
