using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarQube.Client.Api.Requests.V6_60
{
    public class GetNotificationsRequest : RequestBase<SonarQubeNotification[]>, IGetNotificationsRequest
    {
        [JsonProperty("projects")]
        public string ProjectKey { get; set; }

        [JsonProperty("from"), JsonConverter(typeof(JavaDateConverter))]
        public DateTimeOffset EventsSince { get; set; }

        protected override string Path => "api/developers/search_events";

        protected override async Task<Result<SonarQubeNotification[]>> ReadResponse(HttpResponseMessage httpResponse)
        {
            if (httpResponse.IsSuccessStatusCode)
            {
                return await base.ReadResponse(httpResponse);
            }

            var result = httpResponse.StatusCode == HttpStatusCode.NotFound
                ? null // Not supported on server => disable in SLVS
                : new SonarQubeNotification[0];

            httpResponse.StatusCode = HttpStatusCode.OK; // Do not throw in the service

            return new Result<SonarQubeNotification[]>(httpResponse, result);
        }

        protected override SonarQubeNotification[] ParseResponse(string response) =>
            JObject.Parse(response)["events"]
                .ToObject<NotificationsResponse[]>()
                .Select(ToNotification)
                .ToArray();

        private SonarQubeNotification ToNotification(NotificationsResponse response) =>
            new SonarQubeNotification(response.Category, response.Message, response.Link, response.Date);

        private class NotificationsResponse
        {
            [JsonProperty("category")]
            public string Category { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("link")]
            public Uri Link { get; set; }

            [JsonProperty("date")]
            public DateTimeOffset Date { get; set; }

            [JsonProperty("project")]
            public string Project { get; set; }
        }
    }
}
