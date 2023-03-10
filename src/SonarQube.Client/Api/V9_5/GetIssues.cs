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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf;
using Newtonsoft.Json;
using SonarQube.Client.Messages.Issues;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;
using Severity = SonarQube.Client.Messages.Issues.Severity;

namespace SonarQube.Client.Api.V9_5
{
    internal class GetIssuesRequest : RequestBase<SonarQubeIssue[]>, IGetIssuesSinceTimestampRequest
    {
        private static readonly string AllKnownLanguages = string.Join(",", SonarQubeLanguage.AllLanguages.Select(x => x.Key));

        [JsonProperty("projectKey")]
        public string ProjectKey { get; set; }

        [JsonProperty("languages")]
        public string Languages { get; set; } = AllKnownLanguages;

        [JsonIgnore]
        public DateTimeOffset? ChangedSince { get; set; }

        [JsonProperty("changedSince")]
        public long? ChangedSinceUnixTimestamp => ChangedSince?.ToUnixTimeMilliseconds();

        [JsonProperty("branchName")]
        public string Branch { get; set; }

        [JsonProperty("resolvedOnly")] 
        public bool ResolvedOnly { get; set; }

        protected override string Path => "api/issues/pull";

        protected override async Task<Result<SonarQubeIssue[]>> ReadResponseAsync(HttpResponseMessage httpResponse)
        {
            var stream = await httpResponse.Content.ReadAsStreamAsync();

            var _ = IssuesPullQueryTimestamp.Parser.ParseDelimitedFrom(stream);
            var issues = ReadMessages(stream, IssueLite.Parser);

            // we're ignoring Closed issues as the information returned from SQ is not enough for SLVS (i.e. it has no hash)
            var sqIssues = issues.Where(x=> !x.Closed).Select(ConvertToSonarQubeIssue).ToArray();

            return new Result<SonarQubeIssue[]>(httpResponse, sqIssues);
        }

        private static List<T> ReadMessages<T>(Stream input, MessageParser<T> parser)
            where T : IMessage<T>
        {
            var list = new List<T>();

            while (true)
            {
                try
                {
                    var message = parser.ParseDelimitedFrom(input);
                    list.Add(message);
                }
                catch
                {
                    break;
                }
               
            }
            return list;
        }

        protected override SonarQubeIssue[] ParseResponse(string response)
        {
            // should not be called
            throw new InvalidOperationException();
        }

        private static SonarQubeIssue ConvertToSonarQubeIssue(IssueLite issueLite) =>
            new SonarQubeIssue(
                issueKey: issueLite.Key,
                filePath: issueLite.MainLocation?.FilePath,
                hash: issueLite.MainLocation?.TextRange?.Hash,
                message: issueLite.MainLocation?.Message,
                moduleKey: null,
                ruleId: issueLite.RuleKey,
                isResolved: issueLite.Resolved,
                severity: Convert(issueLite.UserSeverity),
                creationTimestamp: DateTimeOffset.FromUnixTimeMilliseconds(issueLite.CreationDate),
                lastUpdateTimestamp: DateTimeOffset.FromUnixTimeMilliseconds(issueLite.CreationDate),
                textRange: issueLite.MainLocation?.TextRange == null
                    ? null
                    : Convert(issueLite.MainLocation?.TextRange),
                flows: null);

        private static SonarQubeIssueSeverity Convert(Severity severity)
        {
            switch (severity)
            {
                case Severity.Info:
                    return SonarQubeIssueSeverity.Info;
                case Severity.Minor:
                    return SonarQubeIssueSeverity.Minor;
                case Severity.Major:
                    return SonarQubeIssueSeverity.Major;
                case Severity.Critical:
                    return SonarQubeIssueSeverity.Critical;
                case Severity.Blocker:
                    return SonarQubeIssueSeverity.Blocker;
                default:
                    return SonarQubeIssueSeverity.Unknown;
            }
        }

        private static IssueTextRange Convert(TextRange mainLocationTextRange) =>
            new IssueTextRange(
                startLine: mainLocationTextRange.StartLine,
                endLine: mainLocationTextRange.EndLine,
                startOffset: mainLocationTextRange.StartLineOffset,
                endOffset: mainLocationTextRange.EndLineOffset);
    }
}
