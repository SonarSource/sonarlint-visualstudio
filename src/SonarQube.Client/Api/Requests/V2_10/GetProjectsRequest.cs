using System;
using System.Linq;
using Newtonsoft.Json;
using SonarQube.Client.Models;
using SonarQube.Client.Helpers;

namespace SonarQube.Client.Api.Requests.V2_10
{
    public class GetProjectsRequest : RequestBase<SonarQubeProject[]>, IGetProjectsRequest
    {
        [JsonIgnore]
        public bool Ascending { get; set; }

        [JsonIgnore]
        public string OrganizationKey { get; set; }

        [JsonIgnore]
        public int Page { get; set; }

        [JsonIgnore]
        public int PageSize { get; set; }

        protected override string Path => "api/projects/index";

        protected override SonarQubeProject[] ParseResponse(string response) =>
            JsonHelper.Deserialize<ProjectResponse[]>(response)
                .Select(ToProject)
                .ToArray();

        private SonarQubeProject ToProject(ProjectResponse projectResponse) =>
            new SonarQubeProject(projectResponse.Key, projectResponse.Name);

        private class ProjectResponse
        {
            [JsonProperty("k")]
            public string Key { get; set; }

            [JsonProperty("nm")]
            public string Name { get; set; }
        }
    }
}
