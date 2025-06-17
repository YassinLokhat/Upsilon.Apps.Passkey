using System.Text.Json;
using Upsilon.Apps.PassKey.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Utils
{
   public class JsonSerializationCenter : ISerializationCenter
   {
      public string Serialize<T>(T toSerialize) where T : notnull
      {
         return JsonSerializer.Serialize<T>(toSerialize);
      }

      public T Deserialize<T>(string toDeserialize) where T : notnull
      {
         T? obj = JsonSerializer.Deserialize<T>(toDeserialize);

         return obj ?? throw new NullReferenceException(nameof(obj));
      }
   }
}
