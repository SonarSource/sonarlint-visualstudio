using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests.V6_30
{
    public class GetPropertiesRequest : RequestBase<SonarQubeProperty[]>, IGetPropertiesRequest
    {
        protected override string Path => "api/settings/values";

        protected override SonarQubeProperty[] ParseResponse(string response) =>
            JObject.Parse(response)["settings"]
                .ToObject<PropertyResponse[]>()
                .Select(ToProperty)
                .ToArray();

        private SonarQubeProperty ToProperty(PropertyResponse arg) =>
            new SonarQubeProperty(arg.Key, arg.Value);

        private class PropertyResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }

            // TODO: fieldValues
        }
    }
}
