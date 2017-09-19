using Newtonsoft.Json;

namespace SonarQube.Client.Models
{
    public class Property
    {
        public string Key { get; }
        public string Value { get; }

        public Property(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public static Property FromDto(PropertyDTO dto)
        {
            return new Property(dto.Key, dto.Value);
        }
    }

    public class PropertyDTO
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
