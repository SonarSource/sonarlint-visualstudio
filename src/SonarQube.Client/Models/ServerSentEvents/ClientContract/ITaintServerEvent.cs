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
using Newtonsoft.Json;
using SonarQube.Client.Api.Common;
using SonarQube.Client.Helpers;

namespace SonarQube.Client.Models.ServerSentEvents.ClientContract
{
    public interface ITaintServerEvent : IServerEvent
    {
        string ProjectKey { get; }
        string Key { get; }
    }

    /// <summary>
    /// Represents TaintVulnerabilityRaised server event information
    /// </summary>
    public interface ITaintVulnerabilityRaisedServerEvent : ITaintServerEvent
    {
        string Branch { get; }
        ITaintIssue Issue { get; }
    }

    internal class TaintVulnerabilityRaisedServerEvent : ITaintVulnerabilityRaisedServerEvent
    {
        [JsonConstructor]
        public TaintVulnerabilityRaisedServerEvent(string projectKey,
            string key,
            string branch,
            string ruleKey,
            [JsonConverter(typeof(MillisecondUnixTimestampDateTimeOffsetConverter))] DateTimeOffset creationDate,
            SonarQubeIssueSeverity severity,
            SonarQubeIssueType type,
            ServerImpact[] impacts,
            Location mainLocation,
            Flow[] flows,
            string ruleDescriptionContextKey)
            : this(projectKey,
                key,
                branch,
                new TaintIssue(key,
                    ruleKey,
                    creationDate,
                    severity,
                    type,
                    CleanCodeTaxonomyHelpers.ToDefaultImpacts(impacts),
                    mainLocation,
                    flows,
                    ruleDescriptionContextKey))
        {
        }

        public TaintVulnerabilityRaisedServerEvent(string projectKey, string key, string branch, ITaintIssue issue)
        {
            ProjectKey = projectKey ?? throw new ArgumentNullException(nameof(projectKey));
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Branch = branch ?? throw new ArgumentNullException(nameof(branch));
            Issue = issue ?? throw new ArgumentNullException(nameof(issue));
        }

        public string ProjectKey { get; }
        public string Key { get; }
        public string Branch { get; }
        public ITaintIssue Issue { get; }
    }

    /// <summary>
    /// Represents TaintVulnerabilityClosed server event information
    /// </summary>
    public interface ITaintVulnerabilityClosedServerEvent : ITaintServerEvent
    {
    }

    internal class TaintVulnerabilityClosedServerEvent : ITaintVulnerabilityClosedServerEvent
    {
        public TaintVulnerabilityClosedServerEvent(string projectKey, string key)
        {
            ProjectKey = projectKey ?? throw new ArgumentNullException(nameof(projectKey));
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public string ProjectKey { get; }
        public string Key { get; }
    }
}
