/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Text;
using Newtonsoft.Json;

namespace SonarQube.Client.Models.ServerSentEvents.ClientContract
{
    /// <summary>
    /// Represents IssueChanged server event information
    /// </summary>
    public interface IIssueChangedServerEvent : IServerEvent
    {
        string ProjectKey { get; }
        bool IsResolved { get; }
        IBranchAndIssueKey[] BranchAndIssueKeys { get; }

        // also has Severity and Type that we don't care about
    }

    public class IssueChangedServerEvent : IIssueChangedServerEvent
    {
        public IssueChangedServerEvent(string projectKey, bool isResolved, BranchAndIssueKey[] issues)
        {
            ProjectKey = projectKey ?? throw new ArgumentNullException(nameof(projectKey));
            IsResolved = isResolved;
            BranchAndIssueKeys = issues ?? throw new ArgumentNullException(nameof(issues));
        }

        [JsonProperty("projectKey")]
        public string ProjectKey { get; }

        [JsonProperty("resolved")]
        public bool IsResolved { get; }

        [JsonProperty("issues")]
        public IBranchAndIssueKey[] BranchAndIssueKeys { get; }

        // Display-friendly override for logging/debugging
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"ProjectKey: {ProjectKey}, IsResolved: {IsResolved}, Issue count: " + BranchAndIssueKeys.Length);
            foreach (var item in BranchAndIssueKeys)
            {
                sb.AppendLine($"\tBranch: {item.BranchName}, IssueKey: {item.IssueKey}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Tuple of the changed issue key in a specific branch
    /// </summary>
    public interface IBranchAndIssueKey
    {
        string BranchName { get; }
        string IssueKey { get; }
    }

    public class BranchAndIssueKey : IBranchAndIssueKey
    {
        public BranchAndIssueKey(string issueKey, string branchName)
        {
            IssueKey = issueKey ?? throw new ArgumentNullException(nameof(issueKey));
            BranchName = branchName ?? throw new ArgumentNullException(nameof(branchName));
        }

        [JsonProperty("branchName")]
        public string BranchName { get; }

        [JsonProperty("issueKey")]
        public string IssueKey { get; }
    }
}
