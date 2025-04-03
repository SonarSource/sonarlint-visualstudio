using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Core.Helpers;

public class CommaSeparatedStringArrayConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is not string[] strings)
        {
            return;
        }

        writer.WriteValue(string.Join(",", strings));
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        var value = reader.Value?.ToString();
        return value?.Split([','], StringSplitOptions.RemoveEmptyEntries);
    }

    public override bool CanConvert(Type objectType) => objectType == typeof(string[]);
}
