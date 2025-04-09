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

        var commaSeparatedList = string.Join(",", TrimValues(strings));
        writer.WriteValue(commaSeparatedList);
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        var commaSeparatedList = reader.Value as string;
        var values = commaSeparatedList?.Split([','], StringSplitOptions.RemoveEmptyEntries);
        return TrimValues(values);
    }

    public override bool CanConvert(Type objectType) => objectType == typeof(string[]);

    private static string[] TrimValues(string[] values) => values?.Select(value => value.Trim()).ToArray();
}
