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
        if (reader.Value is null)
        {
            return existingValue; // return the default if not a valid value
        }

        if (reader.Value is not string commaSeparatedList)
        {
            throw new JsonException(
                string.Format(
                    CoreStrings.CommaSeparatedStringArrayConverter_UnexpectedType,
                    reader.Value.GetType(),
                    typeof(string),
                    reader.Path));
        }

        var values = commaSeparatedList.Split([','], StringSplitOptions.RemoveEmptyEntries);
        return TrimValues(values);
    }

    public override bool CanConvert(Type objectType) => objectType == typeof(string[]);

    private static string[] TrimValues(string[] values) => values?.Select(value => value.Trim()).ToArray();
}
