using System.Linq;
using Newtonsoft.Json;
using SonarQube.Client.Models;
using SonarQube.Client.Helpers;

namespace SonarQube.Client.Api.Requests.V2_10
{
    public class GetPluginsRequest : RequestBase<SonarQubePlugin[]>, IGetPluginsRequest
    {
        protected override string Path => "api/updatecenter/installed_plugins";

        protected override SonarQubePlugin[] ParseResponse(string response) =>
            JsonHelper.Deserialize<PluginResponse[]>(response)
                .Select(ToPlugin)
                .ToArray();

        private SonarQubePlugin ToPlugin(PluginResponse response) =>
            new SonarQubePlugin(response.Key, response.Version);

        private class PluginResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("version")]
            public string Version { get; set; }
        }
    }
}
