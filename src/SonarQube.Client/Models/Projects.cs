using System;
using Newtonsoft.Json;

namespace SonarQube.Client.Models
{
    public class SonarQubeProject
    {
        // Ordinal comparer should be good enough: http://docs.sonarqube.org/display/SONAR/Project+Administration#ProjectAdministration-AddingaProject
        public static readonly StringComparer KeyComparer = StringComparer.Ordinal;

        public string Key { get; }
        public string Name { get; }

        public SonarQubeProject(string key, string name)
        {
            Key = key;
            Name = name;
        }

        public static SonarQubeProject FromDto(ProjectDTO dto)
        {
            return new SonarQubeProject(dto.Key, dto.Name);
        }

        public static SonarQubeProject FromDto(ComponentDTO dto)
        {
            return new SonarQubeProject(dto.Key, dto.Name);
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
