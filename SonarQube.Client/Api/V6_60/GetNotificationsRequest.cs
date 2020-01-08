/*
 * SonarQube Client
 * Copyright (C) 2016-2020 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V6_60
{
    public class GetNotificationsRequest : RequestBase<SonarQubeNotification[]>, IGetNotificationsRequest
    {
        [JsonProperty("projects")]
        public virtual string ProjectKey { get; set; }

        [JsonProperty("from"), JsonConverter(typeof(JavaDateConverter))]
        public virtual DateTimeOffset EventsSince { get; set; }

        protected override string Path => "api/developers/search_events";

        public async override Task<SonarQubeNotification[]> InvokeAsync(HttpClient httpClient, CancellationToken token)
        {
            var result = await InvokeUncheckedAsync(httpClient, token);

            switch (result.StatusCode)
            {
                case HttpStatusCode.OK:
                    return result.Value;

                case HttpStatusCode.NotFound:
                    Logger.Debug("Notifications not enabled on SonarQube. This feature requires Developer Edition or higher.");
                    return null; // Feature is not enabled, null will stop future checks

                default:
                    Logger.Debug($"Unexpected HttpStatusCode {result.StatusCode}.");
                    return new SonarQubeNotification[0]; // Swallow the error and continue to check for notifications
            }
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
