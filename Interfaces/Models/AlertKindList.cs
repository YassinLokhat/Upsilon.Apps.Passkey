using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Notify-preference payload stored on <see cref="ISettings.AlertsToNotify"/>.
   /// </summary>
   [JsonConverter(typeof(AlertKindListJsonConverter))]
   public sealed class AlertKindList : IReadOnlyList<string>
   {
      private readonly string[] _kinds;

      public AlertKindList(IEnumerable<string>? kinds)
         => _kinds = kinds is null ? [] : [.. kinds.Where(static k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.Ordinal)];

      public static AlertKindList Default { get; } = new(AlertKinds.DefaultNotify);

      public string this[int index] => _kinds[index];

      public int Count => _kinds.Length;

      public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)_kinds).GetEnumerator();

      IEnumerator IEnumerable.GetEnumerator() => _kinds.GetEnumerator();

      public string[] ToArray() => [.. _kinds];

      public bool Contains(string kind)
         => _kinds.Contains(kind, StringComparer.Ordinal);

      public override string ToString()
         => string.Join(", ", _kinds);
   }

   public sealed class AlertKindListJsonConverter : JsonConverter<AlertKindList>
   {
      public override AlertKindList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
         switch (reader.TokenType)
         {
            case JsonTokenType.StartArray:
            {
               List<string> kinds = [];
               while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
               {
                  if (reader.TokenType == JsonTokenType.String)
                  {
                     string? value = reader.GetString();
                     if (!string.IsNullOrWhiteSpace(value))
                     {
                        kinds.Add(value);
                     }
                  }
               }

               return new AlertKindList(kinds);
            }

            case JsonTokenType.String:
            {
               // Autosave readable form / activity FieldValue: comma-separated kind ids.
               string? raw = reader.GetString();
               if (string.IsNullOrWhiteSpace(raw))
               {
                  return new AlertKindList([]);
               }

               return new AlertKindList(
                  raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            case JsonTokenType.Null:
               return new AlertKindList([]);

            default:
               throw new JsonException($"Unexpected token for AlertsToNotify: {reader.TokenType}.");
         }
      }

      public override void Write(Utf8JsonWriter writer, AlertKindList value, JsonSerializerOptions options)
      {
         ArgumentNullException.ThrowIfNull(writer);

         writer.WriteStartArray();
         foreach (string kind in value ?? new AlertKindList([]))
         {
            writer.WriteStringValue(kind);
         }

         writer.WriteEndArray();
      }
   }
}
