using Newtonsoft.Json;

namespace SonarQube.Client.Models
{
    public class SonarQubePlugin
    {
        public string Key { get; }
        public string Version { get; }

        public SonarQubePlugin(string key, string version)
        {
            Key = key;
            Version = version;
        }

        public static SonarQubePlugin FromDto(PluginDTO dto)
        {
            return new SonarQubePlugin(dto.Key, dto.Version);
        }
    }

    public class PluginDTO
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
