using System.IO;
using Newtonsoft.Json;
using SonarQube.Client.Messages;

namespace SonarQube.Client.Api.Requests.V5_20
{
    public class GetRoslynExportProfileRequest : RequestBase<RoslynExportProfileResponse>, IGetRoslynExportProfileRequest
    {
        [JsonProperty("language")]
        public string LanguageKey { get; set; }

        [JsonProperty("profileName")]
        public virtual string QualityProfileName { get; set; }

        [JsonProperty("exporterKey", Required = Required.Always)]
        public string ExporterKey => $"roslyn-{LanguageKey}";

        protected override string Path => "api/qualityprofiles/export";

        protected override RoslynExportProfileResponse ParseResponse(string response)
        {
            using (var reader = new StringReader(response))
            {
                // TODO: consider not returning the xml directly
                return RoslynExportProfileResponse.Load(reader);
            }
        }
    }
}
