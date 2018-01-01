using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarQube.Client.Api.Requests.V5_20
{
    public class GetQualityProfilesRequest : RequestBase<SonarQubeQualityProfile[]>, IGetQualityProfilesRequest
    {
        [JsonProperty("projectKey")]
        public virtual string ProjectKey { get; set; }

        [JsonProperty("organization")]
        public string OrganizationKey { get; set; }

        [JsonProperty("defaults")]
        public bool? Defaults => string.IsNullOrWhiteSpace(ProjectKey) ? (bool?)true : null;

        protected override string Path => "api/qualityprofiles/search";

        protected async override Task<Result<SonarQubeQualityProfile[]>> InvokeImplAsync(HttpClient httpClient, CancellationToken token)
        {
            var result = await base.InvokeImplAsync(httpClient, token);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                // The project has not been scanned yet, get default quality profile
                ProjectKey = null;

                result = await base.InvokeImplAsync(httpClient, token);
            }

            return result;
        }

        protected override SonarQubeQualityProfile[] ParseResponse(string response) =>
            JObject.Parse(response)["profiles"]
                .ToObject<QualityProfileResponse[]>()
                .Select(FromResponse)
                .ToArray();

        private static SonarQubeQualityProfile FromResponse(QualityProfileResponse response) =>
            new SonarQubeQualityProfile(response.Key, response.Name, response.Language, response.IsDefault, default(DateTime));

        // This class MUST NOT change! If a breaking change in the API is introduced,
        // create a new versioned request and reimplement the serialization there
        private class QualityProfileResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("language")]
            public string Language { get; set; }

            [JsonProperty("isDefault")]
            public bool IsDefault { get; set; }
        }
    }
}
