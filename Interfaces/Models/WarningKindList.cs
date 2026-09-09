using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Notify-preference payload stored on <see cref="ISettings.WarningsToNotify"/>.
   /// Distinct from <c>string[]</c> so JSON can migrate legacy <see cref="WarningType"/> flags.
   /// </summary>
   [JsonConverter(typeof(WarningKindListJsonConverter))]
   public sealed class WarningKindList : IReadOnlyList<string>
   {
      private readonly string[] _kinds;

      public WarningKindList(IEnumerable<string>? kinds)
         => _kinds = kinds is null ? [] : [.. kinds.Where(static k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.Ordinal)];

      public static WarningKindList Default { get; } = new(WarningKinds.DefaultNotify);

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

   public sealed class WarningKindListJsonConverter : JsonConverter<WarningKindList>
   {
      public override WarningKindList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

               return new WarningKindList(kinds);
            }

            case JsonTokenType.String:
            {
               string? raw = reader.GetString();
               if (string.IsNullOrWhiteSpace(raw))
               {
                  return new WarningKindList([]);
               }

#pragma warning disable CS0618 // Legacy WarningType migration
               if (Enum.TryParse(raw, ignoreCase: true, out WarningType legacyFromName))
               {
                  return new WarningKindList(WarningKinds.FromLegacyWarningType(legacyFromName));
               }
#pragma warning restore CS0618

               return new WarningKindList(
                  raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            case JsonTokenType.Number:
            {
#pragma warning disable CS0618 // Legacy WarningType migration
               return new WarningKindList(
                  WarningKinds.FromLegacyWarningType((WarningType)reader.GetInt32()));
#pragma warning restore CS0618
            }

            case JsonTokenType.Null:
               return new WarningKindList([]);

            default:
               throw new JsonException($"Unexpected token for WarningsToNotify: {reader.TokenType}.");
         }
      }

      public override void Write(Utf8JsonWriter writer, WarningKindList value, JsonSerializerOptions options)
      {
         ArgumentNullException.ThrowIfNull(writer);

         writer.WriteStartArray();
         foreach (string kind in value ?? new WarningKindList([]))
         {
            writer.WriteStringValue(kind);
         }

         writer.WriteEndArray();
      }
   }
}
