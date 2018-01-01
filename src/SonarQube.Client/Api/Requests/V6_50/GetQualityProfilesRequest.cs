using Newtonsoft.Json;

namespace SonarQube.Client.Api.Requests.V6_50
{
    public class GetQualityProfilesRequest : V5_20.GetQualityProfilesRequest
    {
        [JsonProperty("project")]
        public override string ProjectKey { get; set; }
    }
}
