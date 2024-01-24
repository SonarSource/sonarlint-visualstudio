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
using System.Collections.Generic;

namespace SonarQube.Client.Models
{
    public class SonarQubeIssue
    {
        private static readonly IReadOnlyList<IssueFlow> EmptyFlows = new List<IssueFlow>().AsReadOnly();

        public SonarQubeIssue(string issueKey, string filePath, string hash, string message, string moduleKey, string ruleId, bool isResolved,
            SonarQubeIssueSeverity severity, DateTimeOffset creationTimestamp, DateTimeOffset lastUpdateTimestamp,
            IssueTextRange textRange, List<IssueFlow> flows, string context = null, SonarQubeCleanCodeAttribute? cleanCodeAttribute = null,
            Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity> defaultImpacts = null)
        {
            IssueKey = issueKey;
            FilePath = filePath;
            Hash = hash;
            Message = message;
            ModuleKey = moduleKey;
            RuleId = ruleId;
            IsResolved = isResolved;
            Severity = severity;
            CreationTimestamp = creationTimestamp;
            LastUpdateTimestamp = lastUpdateTimestamp;
            TextRange = textRange;
            Flows = flows ?? EmptyFlows;
            Context = context;
            CleanCodeAttribute = cleanCodeAttribute;
            DefaultImpacts = defaultImpacts;
        }

        public string IssueKey { get; }

        /// <summary>
        /// Relative file path
        /// </summary>
        /// <remarks>
        /// The path is relative to the Sonar project root.
        /// The path is in Windows format i.e. the directory separators are backslashes
        /// </remarks>
        public string FilePath { get; }

        public string Hash { get; }
        public string Message { get; }
        public string ModuleKey { get; }
        public string RuleId { get; }

        /// <remarks>
        /// This needs to be mutable as SLVS will update it during runtime.
        /// </remarks>
        public bool IsResolved { get; set; }

        public SonarQubeIssueSeverity Severity { get; }
        public IssueTextRange TextRange { get; }
        public DateTimeOffset CreationTimestamp { get; }
        public DateTimeOffset LastUpdateTimestamp { get; }
        public IReadOnlyList<IssueFlow> Flows { get; }

        public string Context { get; set; }

        public SonarQubeCleanCodeAttribute? CleanCodeAttribute { get; }

        public Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity> DefaultImpacts { get; set; }
    }

    public class IssueFlow
    {
        private static readonly IReadOnlyList<IssueLocation> EmptyLocations = new List<IssueLocation>().AsReadOnly();

        public IssueFlow(List<IssueLocation> locations)
        {
            Locations = locations ?? EmptyLocations;
        }

        public IReadOnlyList<IssueLocation> Locations { get; }
    }

    public class IssueLocation
    {
        public IssueLocation(string filePath, string moduleKey, IssueTextRange textRange, string message)
        {
            FilePath = filePath;
            ModuleKey = moduleKey;
            TextRange = textRange;
            Message = message;
        }

        public string FilePath { get; }
        public string ModuleKey { get; }
        public IssueTextRange TextRange { get; }
        public string Message { get; }

        /// <summary>
        /// Note: currently the hash for secondary locations is calculated
        /// post-construction, so we need to be able to update this property
        /// </summary>
        public string Hash { get; internal set; }
    }

    public class IssueTextRange
    {
        public IssueTextRange(int startLine, int endLine, int startOffset, int endOffset)
        {
            StartLine = startLine;
            EndLine = endLine;
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public int StartLine { get; }
        public int EndLine { get; }
        public int StartOffset { get; }
        public int EndOffset { get; }
    }
}
