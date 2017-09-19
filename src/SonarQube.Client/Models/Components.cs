using Newtonsoft.Json;

namespace SonarQube.Client.Models
{
    public class ComponentDTO
    {
        [JsonProperty("organization")]
        public string Organization { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class ComponentRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string OrganizationKey { get; set; }
    }
}
