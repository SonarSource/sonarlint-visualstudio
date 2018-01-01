using System.Linq;
using Newtonsoft.Json;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests.V2_60
{
    public class GetPropertiesRequest : RequestBase<SonarQubeProperty[]>, IGetPropertiesRequest
    {
        protected override string Path => "api/properties";

        protected override SonarQubeProperty[] ParseResponse(string response) =>
            JsonHelper.Deserialize<PropertyResponse[]>(response)
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
        }
    }
}
