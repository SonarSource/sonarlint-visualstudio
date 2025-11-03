/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

namespace SonarLint.VisualStudio.Core.Analysis
{
    public class AnalysisIssue(
        Guid? id,
        string ruleKey,
        string issueServerKey,
        bool isResolved,
        bool isOnNewCode,
        AnalysisIssueSeverity? severity,
        AnalysisIssueType? type,
        Impact highestImpact,
        IAnalysisIssueLocation primaryLocation,
        IReadOnlyList<IAnalysisIssueFlow> flows,
        IReadOnlyList<IQuickFixBase> fixes = null)
        : IAnalysisIssue
    {
        private static readonly IReadOnlyList<IAnalysisIssueFlow> EmptyFlows = [];
        private static readonly IReadOnlyList<IQuickFixBase> EmptyFixes = [];

        public Guid? Id { get; } = id;

        public string RuleKey { get; } = ruleKey;

        public AnalysisIssueSeverity? Severity { get; } = severity;

        public AnalysisIssueType? Type { get; } = type;

        public IReadOnlyList<IAnalysisIssueFlow> Flows { get; } = flows ?? EmptyFlows;

        public IAnalysisIssueLocation PrimaryLocation { get; } = primaryLocation ?? throw new ArgumentNullException(nameof(primaryLocation));
        public bool IsResolved { get; } = isResolved;

        public bool IsOnNewCode { get; } = isOnNewCode;
        public string IssueServerKey { get; } = issueServerKey;

        public IReadOnlyList<IQuickFixBase> Fixes { get; } = fixes ?? EmptyFixes;
        public Impact HighestImpact { get; } = highestImpact;
    }

    public class AnalysisHotspotIssue(
        Guid? id,
        string ruleKey,
        string issueServerKey,
        bool isResolved,
        bool isOnNewCode,
        AnalysisIssueSeverity? severity,
        AnalysisIssueType? type,
        Impact highestImpact,
        IAnalysisIssueLocation primaryLocation,
        IReadOnlyList<IAnalysisIssueFlow> flows,
        HotspotStatus hotspotStatus,
        IReadOnlyList<IQuickFixBase> fixes = null,
        HotspotPriority? hotspotPriority = null)
        : AnalysisIssue(id, ruleKey, issueServerKey, isResolved, isOnNewCode, severity, type, highestImpact, primaryLocation, flows, fixes), IAnalysisHotspotIssue
    {
        public HotspotPriority? HotspotPriority { get; } = hotspotPriority;
        public HotspotStatus HotspotStatus { get; } = hotspotStatus;
    }

    public class AnalysisIssueFlow(IReadOnlyList<IAnalysisIssueLocation> locations) : IAnalysisIssueFlow
    {
        public IReadOnlyList<IAnalysisIssueLocation> Locations { get; } = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    public class AnalysisIssueLocation(string message, string filePath, ITextRange textRange) : IAnalysisIssueLocation
    {
        public string FilePath { get; } = filePath;

        public string Message { get; } = message;

        public ITextRange TextRange { get; } = textRange;
    }

    public class TextRange(
        int startLine,
        int endLine,
        int startLineOffset,
        int endLineOffset,
        string lineHash)
        : ITextRange
    {
        public int StartLine { get; } = startLine;
        public int EndLine { get; } = endLine;
        public int StartLineOffset { get; } = startLineOffset;
        public int EndLineOffset { get; } = endLineOffset;
        public string LineHash { get; } = lineHash;
    }
}
