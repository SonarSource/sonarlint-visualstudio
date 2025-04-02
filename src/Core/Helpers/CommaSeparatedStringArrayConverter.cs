using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Core.Helpers;

public class CommaSeparatedStringArrayConverter : JsonConverter<string[]>
{
    public override void WriteJson(JsonWriter writer, string[] value, JsonSerializer serializer) => writer.WriteValue(string.Join(",", value));

    public override string[] ReadJson(
        JsonReader reader,
        Type objectType,
        string[] existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var value = reader.Value?.ToString();
        return value?.Split([','], StringSplitOptions.RemoveEmptyEntries) ?? [];
    }
}
