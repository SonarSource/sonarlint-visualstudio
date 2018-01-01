using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SonarQube.Client.Api.Requests.V5_20
{
    public class GetQualityProfileChangeLogRequest : PagedRequestBase<DateTime>, IGetQualityProfileChangeLogRequest
    {
        [JsonProperty("profileKey")]
        public string QualityProfileKey { get; set; }

        protected override string Path => "api/qualityprofiles/changelog";

        protected override DateTime[] ParseResponse(string response) =>
            JObject.Parse(response)["events"]
                .ToObject<QualityProfileChangeItemResponse[]>()
                .Select(x => x.Date)
                .ToArray();

        private class QualityProfileChangeItemResponse
        {
            [JsonProperty("date")]
            public DateTime Date { get; set; }
        }
    }
}
