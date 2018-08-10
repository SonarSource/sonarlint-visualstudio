/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf;
using Newtonsoft.Json;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarQube.Client.Api.V5_10
{
    public class GetIssuesRequest : RequestBase<SonarQubeIssue[]>, IGetIssuesRequest
    {
        [JsonProperty("key")]
        public virtual string ProjectKey { get; set; }

        protected override string Path => "batch/issues";

        protected async override Task<Result<SonarQubeIssue[]>> ReadResponseAsync(HttpResponseMessage httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                return new Result<SonarQubeIssue[]>(httpResponse, null);
            }

            var byteArray = await httpResponse.Content.ReadAsByteArrayAsync();
            // Protobuf for C# throws when trying to read outside of the buffer and ReadAsStreamAsync returns a non
            // seekable stream so we can't determine when to stop. The hack is to use an intermediate MemoryStream
            // so we can control when to stop reading.
            // Note we might want to use FileStream instead to avoid intensive memory usage.
            using (var stream = new MemoryStream(byteArray))
            {
                return new Result<SonarQubeIssue[]>(httpResponse,
                    ReadFromProtobufStream(stream, ServerIssue.Parser).Select(ToSonarQubeIssue).ToArray());
            }
        }

        protected override SonarQubeIssue[] ParseResponse(string response)
        {
            throw new NotSupportedException("This method will not be called because we override ReadResponse.");
        }

        private static IEnumerable<T> ReadFromProtobufStream<T>(Stream stream, MessageParser<T> parser)
            where T : IMessage<T>
        {
            while (stream.Position < stream.Length)
            {
                yield return parser.ParseDelimitedFrom(stream);
            }
        }

        private static SonarQubeIssue ToSonarQubeIssue(ServerIssue issue) =>
            new SonarQubeIssue(issue.Path, issue.Checksum, issue.Line, issue.Msg, issue.ModuleKey,
                ParseResolutionState(issue.Resolution), issue.RuleKey);

        private static SonarQubeIssueResolutionState ParseResolutionState(string resolution)
        {
            switch (resolution)
            {
                case "":
                    return SonarQubeIssueResolutionState.Unresolved;
                case "WONTFIX":
                    return SonarQubeIssueResolutionState.WontFix;
                case "FALSE-POSITIVE":
                    return SonarQubeIssueResolutionState.FalsePositive;
                case "FIXED":
                    return SonarQubeIssueResolutionState.Fixed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution));
            }
        }
    }
}
