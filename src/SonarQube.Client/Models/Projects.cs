using Newtonsoft.Json;

namespace SonarQube.Client.Models
{
    public class Project
    {
        public string Key { get; }
        public string Name { get; }

        public Project(string key, string name)
        {
            Key = key;
            Name = name;
        }

        public static Project FromDto(ProjectDTO dto)
        {
            return new Project(dto.Key, dto.Name);
        }

        public static Project FromDto(ComponentDTO dto)
        {
            return new Project(dto.Key, dto.Name);
        }
    }

    public class ProjectDTO
    {
        [JsonProperty("k")]
        public string Key { get; set; }

        [JsonProperty("nm")]
        public string Name { get; set; }
    }
}
