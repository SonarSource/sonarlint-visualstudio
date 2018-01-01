using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests.V6_20
{
    public class GetOrganizationsRequest : PagedRequestBase<SonarQubeOrganization>, IGetOrganizationsRequest
    {
        protected override string Path => "api/organizations/search";

        protected override SonarQubeOrganization[] ParseResponse(string response) =>
            JObject.Parse(response)["organizations"]
                .ToObject<OrganizationResponse[]>()
                .Select(ToOrganization)
                .ToArray();

        private SonarQubeOrganization ToOrganization(OrganizationResponse response) =>
            new SonarQubeOrganization(response.Key, response.Name);

        private class OrganizationResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }
    }
}
