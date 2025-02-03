using System.Text.Json;
using Upsilon.Apps.PassKey.Core.Abstraction.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Implementation.Utils
{
   /// <summary>
   /// Represent a Json implementation of the serialization center.
   /// </summary>
   public class JsonSerializationCenter : ISerializationCenter
   {
      /// <summary>
      /// Serialize the given object to a string.
      /// </summary>
      /// <typeparam name="T">The type of the object.</typeparam>
      /// <param name="toSerialize">The object to serialize.</param>
      /// <returns>The serialised string.</returns>
      public string Serialize<T>(T toSerialize) where T : notnull
      {
         return JsonSerializer.Serialize<T>(toSerialize);
      }

      /// <summary>
      /// Deserialize the given string to the given type object.
      /// </summary>
      /// <typeparam name="T">The type of the object.</typeparam>
      /// <param name="toDeserialize">The serialised string.</param>
      /// <returns>The deserialized object.</returns>
      public T Deserialize<T>(string toDeserialize) where T : notnull
      {
         T? obj = JsonSerializer.Deserialize<T>(toDeserialize);

         return obj ?? throw new NullReferenceException(nameof(obj));
      }
   }
}
