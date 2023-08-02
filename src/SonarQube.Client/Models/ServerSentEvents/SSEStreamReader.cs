/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;
using SonarQube.Client.Models.ServerSentEvents.ServerContract;
using System.Collections.Generic;
using SonarQube.Client.Logging;

namespace SonarQube.Client.Models.ServerSentEvents
{
    public interface ISSEStreamReader
    {
        /// <summary>
        /// Wraps the stream response from the server, reads from it and converts it to <see cref="IServerEvent"/>.
        /// Will block the calling thread until an event exists or the connection is closed.
        /// </summary>
        /// <returns>
        /// Can return null (i.e. if the underlying event type is unsupported).
        /// </returns>
        Task<IServerEvent> ReadAsync();
    }

    /// <summary>
    /// Returns <see cref="IServerEvent"/> deserialized from <see cref="ISqServerEvent"/>
    /// Code on the java side: https://github.com/SonarSource/sonarlint-core/blob/4f34c7c844b12e331a61c63ad7105acac41d2efd/server-api/src/main/java/org/sonarsource/sonarlint/core/serverapi/push/PushApi.java
    /// </summary>
    internal class SSEStreamReader : ISSEStreamReader
    {
        private readonly ISqSSEStreamReader sqSSEStreamReader;
        private readonly ILogger logger;

        private readonly IDictionary<string, Type> eventTypeToDataTypeMap = new Dictionary<string, Type>
        {
            {"IssueChanged", typeof(IssueChangedServerEvent)},
            {"TaintVulnerabilityClosed", typeof(TaintVulnerabilityClosedServerEvent)},
            {"TaintVulnerabilityRaised", typeof(TaintVulnerabilityRaisedServerEvent)},
            {"RuleSetChanged", typeof(QualityProfileEvent)},
        };

        public SSEStreamReader(ISqSSEStreamReader sqSSEStreamReader, ILogger logger)
        {
            this.sqSSEStreamReader = sqSSEStreamReader;
            this.logger = logger;
        }

        public async Task<IServerEvent> ReadAsync()
        {
            var sqEvent = await ReadNextEventAsync();

            if (sqEvent == null || !eventTypeToDataTypeMap.ContainsKey(sqEvent.Type))
            {
                return null;
            }

            try
            {
                var deserializedEvent = JsonConvert.DeserializeObject(sqEvent.Data, eventTypeToDataTypeMap[sqEvent.Type]);

                return (IServerEvent) deserializedEvent;
            }
            catch (Exception ex)
            {
                logger.Debug("[SSEStreamReader] Failed to deserialize sq event." +
                             $"\n Exception: {ex}" +
                             $"\n Raw event type: {sqEvent.Type}" +
                             $"\n Raw event data: {sqEvent.Data}");

                return null;
            }
        }

        private async Task<ISqServerEvent> ReadNextEventAsync()
        {
            try
            {
                return await sqSSEStreamReader.ReadAsync();
            }
            catch (Exception)
            {
                sqSSEStreamReader.Dispose();
                throw;
            }
        }
    }
}
