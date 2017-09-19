using Newtonsoft.Json;

namespace SonarQube.Client.Models
{
    public class Plugin
    {
        public string Key { get; }
        public string Version { get; }

        public Plugin(string key, string version)
        {
            Key = key;
            Version = version;
        }

        public static Plugin FromDto(PluginDTO dto)
        {
            return new Plugin(dto.Key, dto.Version);
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
