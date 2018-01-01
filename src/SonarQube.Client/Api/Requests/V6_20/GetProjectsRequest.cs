using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests.V6_20
{
    public class GetProjectsRequest : PagedRequestBase<SonarQubeProject>, IGetProjectsRequest
    {
        [JsonProperty("organization")]
        public string OrganizationKey { get; set; }

        [JsonProperty("asc")]
        public bool Ascending { get; set; } = true;

        protected override string Path => "api/components/search_projects";

        protected override SonarQubeProject[] ParseResponse(string response) =>
            JObject.Parse(response)["components"]
                .ToObject<ComponentResponse[]>()
                .Select(ToProject)
                .ToArray();

        private SonarQubeProject ToProject(ComponentResponse response) =>
            new SonarQubeProject(response.Key, response.Name);

        private class ComponentResponse
        {
            [JsonProperty("organization")]
            public string Organization { get; set; }

            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }
    }
}
