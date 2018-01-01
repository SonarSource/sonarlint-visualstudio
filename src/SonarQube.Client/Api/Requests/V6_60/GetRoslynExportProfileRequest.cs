using Newtonsoft.Json;

namespace SonarQube.Client.Api.Requests.V6_60
{
    public class GetRoslynExportProfileRequest : V5_20.GetRoslynExportProfileRequest
    {
        [JsonProperty("qualityProfile")]
        public override string QualityProfileName { get; set; }
    }
}
