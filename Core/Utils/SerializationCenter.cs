using System.Text.Json;

namespace Upsilon.Apps.PassKey.Core.Utils
{
   public static class SerializationCenter
   {
      public static string Serialize<T>(this T toSerialize, bool indent = false) where T : notnull
      {
         JsonSerializerOptions options = new()
         {
            WriteIndented = indent,
         };

         return JsonSerializer.Serialize(toSerialize, toSerialize.GetType(), options);
      }

      public static T Deserialize<T>(this string toDeserialize)
      {
         T? obj = JsonSerializer.Deserialize<T>(toDeserialize);

         return obj ?? throw new NullReferenceException(nameof(obj));
      }
   }
}
