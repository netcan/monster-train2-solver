using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static class ModelJson
{
    internal static readonly JsonSerializerOptions Options = Create();
    internal static FixtureDocument ReadFixture(string path) => path.EndsWith(".mt2f", StringComparison.OrdinalIgnoreCase)
        ? FixtureDocument.Read(path) : LegacyFixtureImport.Read(path);
    internal static string? Difference(string predicted, string actual)
    {
        using var left = JsonDocument.Parse(predicted);
        using var right = JsonDocument.Parse(actual);
        return Find(left.RootElement, right.RootElement, "$");
        static string? Find(JsonElement a, JsonElement b, string path)
        {
            if (a.ValueKind != b.ValueKind) return path + ": " + a.ValueKind + " != " + b.ValueKind;
            if (a.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in b.EnumerateObject())
                    if (!a.TryGetProperty(property.Name, out _)) return path + "." + property.Name + ": unexpected";
                foreach (var property in a.EnumerateObject())
                {
                    if (!b.TryGetProperty(property.Name, out var value)) return path + "." + property.Name + ": missing";
                    string? difference = Find(property.Value, value, path + "." + property.Name);
                    if (difference != null) return difference;
                }
                return null;
            }
            if (a.ValueKind == JsonValueKind.Array)
            {
                if (a.GetArrayLength() != b.GetArrayLength()) return path + ".Count: " + a.GetArrayLength() + " != " + b.GetArrayLength();
                for (int i = 0; i < a.GetArrayLength(); i++)
                {
                    string? difference = Find(a[i], b[i], path + "[" + i + "]");
                    if (difference != null) return difference;
                }
                return null;
            }
            return a.GetRawText() == b.GetRawText() ? null : path + ": " + a.GetRawText() + " != " + b.GetRawText();
        }
    }
    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new RngConverter());
        return options;
    }
    private sealed class RngConverter : JsonConverter<UnityRng>
    {
        public override UnityRng Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            JsonElement state = document.RootElement;
            return new UnityRng(state.GetProperty("S0").GetUInt32(), state.GetProperty("S1").GetUInt32(),
                state.GetProperty("S2").GetUInt32(), state.GetProperty("S3").GetUInt32());
        }
        public override void Write(Utf8JsonWriter writer, UnityRng value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("S0", value.S0); writer.WriteNumber("S1", value.S1);
            writer.WriteNumber("S2", value.S2); writer.WriteNumber("S3", value.S3);
            writer.WriteEndObject();
        }
    }
}
