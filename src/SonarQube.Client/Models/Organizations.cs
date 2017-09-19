
using Newtonsoft.Json;

namespace SonarQube.Client.Models
{
    public class Organization
    {
        public string Key { get; }
        public string Name { get; }

        public Organization(string key, string name)
        {
            Key = key;
            Name = name;
        }

        public static Organization FromDto(OrganizationDTO dto)
        {
            return new Organization(dto.Key, dto.Name);
        }
    }

    public class OrganizationDTO
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class OrganizationRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
