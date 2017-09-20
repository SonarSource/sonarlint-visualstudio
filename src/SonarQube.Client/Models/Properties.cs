using Newtonsoft.Json;

namespace SonarQube.Client.Models
{
    public class SonarQubeProperty
    {
        public string Key { get; }
        public string Value { get; }

        public SonarQubeProperty(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public static SonarQubeProperty FromDto(PropertyDTO dto)
        {
            return new SonarQubeProperty(dto.Key, dto.Value);
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
