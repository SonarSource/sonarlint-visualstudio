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
    public interface IAnalysisIssue : IAnalysisIssueBase
    {
        AnalysisIssueSeverity? Severity { get; }

        AnalysisIssueType? Type { get; }

        IReadOnlyList<IQuickFixBase> Fixes { get; }

        Impact HighestImpact { get; }
    }

    public interface IAnalysisHotspotIssue : IAnalysisIssue
    {
        HotspotPriority? HotspotPriority { get; }
        HotspotStatus HotspotStatus { get; }
    }

    public interface IAnalysisIssueBase
    {
        /// <summary>
        /// The id of the issue that comes from SlCore
        /// </summary>
        Guid? Id { get; }

        string RuleKey { get; }

        IReadOnlyList<IAnalysisIssueFlow> Flows { get; }

        /// <summary>
        /// Should never be null
        /// </summary>
        IAnalysisIssueLocation PrimaryLocation { get; }

        /// <summary>
        /// If the issue has been resolved/accepted on the server
        /// </summary>
        bool IsResolved { get; }

        bool IsOnNewCode { get; }

        /// <summary>
        /// The key of the issue on the server
        /// </summary>
        string IssueServerKey { get; }
    }

    public interface IAnalysisIssueFlow
    {
        IReadOnlyList<IAnalysisIssueLocation> Locations { get; }
    }

    public interface IAnalysisIssueLocation
    {
        string FilePath { get; }

        string Message { get; }

        ITextRange TextRange { get; }
    }

    /// <summary>
    /// Represents a Sonar text range
    /// </summary>
    public interface ITextRange
    {
        /// <summary>
        /// 1-based line
        /// </summary>
        int StartLine { get; }

        /// <summary>
        /// 1-based line
        /// </summary>
        int EndLine { get; }

        /// <summary>
        /// 0-based column
        /// </summary>
        int StartLineOffset { get; }

        /// <summary>
        /// 0-based column
        /// </summary>
        int EndLineOffset { get; }

        string LineHash { get; }
    }

    public enum AnalysisIssueSeverity
    {
        Blocker,
        Critical,
        Major,
        Minor,
        Info,
    }

    public enum AnalysisIssueType
    {
        CodeSmell,
        Bug,
        Vulnerability,
        SecurityHotspot
    }

    public enum HotspotPriority
    {
        High,
        Medium,
        Low
    }

    public enum HotspotStatus
    {
        ToReview,
        Acknowledged,
        Fixed,
        Safe,
    }

    public enum DependencyRiskImpactSeverity
    {
        Info,
        Low,
        Medium,
        High,
        Blocker
    }

    public enum DependencyRiskType
    {
        Vulnerability,
        ProhibitedLicense
    }

    public enum DependencyRiskStatus
    {
        Fixed,
        Open,
        Confirmed,
        Accepted,
        Safe
    }

    public enum DependencyRiskTransition
    {
        Confirm,
        Reopen,
        Safe,
        Fixed,
        Accept
    }

    public static class IAnalysisIssueExtensions
    {
        public static bool IsFileLevel(this IAnalysisIssueBase issue)
        {
            return issue.PrimaryLocation.TextRange == null;
        }
    }
}
