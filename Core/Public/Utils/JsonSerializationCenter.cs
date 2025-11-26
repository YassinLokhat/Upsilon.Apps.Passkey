using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Upsilon.Apps.Passkey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.Core.Public.Utils
{
   public class JsonSerializationCenter : ISerializationCenter
   {
      private static readonly JsonSerializerOptions _options = new() { Converters = { new JsonStringEnumConverter() }, };

      public string Serialize<T>(T toSerialize) where T : notnull
         => JsonSerializer.Serialize<T>(toSerialize, _options);

      public T Deserialize<T>(string toDeserialize) where T : notnull
         => JsonSerializer.Deserialize<T>(toDeserialize, _options) ?? throw new NullReferenceException(nameof(toDeserialize));
   }
}
