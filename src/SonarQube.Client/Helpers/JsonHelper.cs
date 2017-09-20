using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace SonarQube.Client.Helpers
{
    public static class JsonHelper
    {
        public static T Deserialize<T>(string json)
        {
            using (var reader = new StringReader(json))
            {
                using (var textReader = new JsonTextReader(reader))
                {
                    return JsonSerializer.CreateDefault().Deserialize<T>(textReader);
                }
            }
        }

        public static string Serialize(object item)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                using (var textWriter = new JsonTextWriter(writer))
                {
                    var serializer = JsonSerializer.CreateDefault();
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(textWriter, item);
                }
            }

            return sb.ToString();
        }
    }
}
