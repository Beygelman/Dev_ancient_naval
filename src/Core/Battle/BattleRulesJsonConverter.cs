using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevAncientNaval.Core.Battle;

/// <summary>
/// AOT-safe rules reader retaining initializer defaults for absent historical
/// fields. Session-envelope generated contexts must register this converter too.
/// </summary>
public sealed class BattleRulesJsonConverter : JsonConverter<BattleRules>
{
    // .NET 8 generated init-only setters are implemented as a constructor-like
    // initializer assigning every property, even those absent from JSON. Supply
    // the existing object defaults first, without making immutable rules mutable
    // or treating explicit null/zero as an absent field.
    private static readonly JsonDocument Defaults = JsonDocument.Parse(
        JsonSerializer.Serialize(new BattleRules(), BattleRulesJsonContext.Default.BattleRules));
    private static readonly BattleRulesJsonContext StrictContext = new(
        new JsonSerializerOptions(BattleRulesJsonContext.Default.Options) { PropertyNameCaseInsensitive = false });

    public override BattleRules? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            WriteWithDefaults(writer, document.RootElement, Defaults.RootElement, options.PropertyNameCaseInsensitive);
        var context = options.PropertyNameCaseInsensitive ? BattleRulesJsonContext.Default : StrictContext;
        return JsonSerializer.Deserialize(stream.ToArray(), context.BattleRules);
    }

    public override void Write(Utf8JsonWriter writer, BattleRules value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, BattleRulesJsonContext.Default.BattleRules);

    internal static BattleRules? Read(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        var result = new BattleRulesJsonConverter().Read(ref reader, typeof(BattleRules),
            BattleRulesJsonContext.Default.Options);
        // Match the normal serializer's rejection of additional JSON values.
        if (reader.Read())
            throw new JsonException("Additional JSON content after battle rules.");
        return result;
    }

    private static void WriteWithDefaults(Utf8JsonWriter writer, JsonElement actual, JsonElement defaults, bool insensitive)
    {
        if (actual.ValueKind != JsonValueKind.Object || defaults.ValueKind != JsonValueKind.Object)
        {
            actual.WriteTo(writer);
            return;
        }
        var comparison = insensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var supplied = actual.EnumerateObject().ToArray();
        var fallback = defaults.EnumerateObject().ToArray();
        writer.WriteStartObject();
        foreach (var property in supplied)
        {
            writer.WritePropertyName(property.Name);
            int index = Array.FindIndex(fallback, p => string.Equals(p.Name, property.Name, comparison));
            if (index >= 0)
                WriteWithDefaults(writer, property.Value, fallback[index].Value, insensitive);
            else
                property.Value.WriteTo(writer);
        }
        foreach (var property in fallback)
            if (!supplied.Any(p => string.Equals(p.Name, property.Name, comparison)))
                property.WriteTo(writer);
        writer.WriteEndObject();
    }
}
