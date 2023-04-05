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

using System.Collections.Generic;

namespace SonarLint.VisualStudio.Core.Analysis
{
    public interface IAnalysisIssue : IAnalysisIssueBase
    {
        AnalysisIssueSeverity Severity { get; }

        AnalysisIssueType Type { get; }

        IReadOnlyList<IQuickFix> Fixes { get; }
    }

    public interface IAnalysisIssueBase
    {
        string RuleKey { get; }

        IReadOnlyList<IAnalysisIssueFlow> Flows { get; }

        /// <summary>
        /// Should never be null
        /// </summary>
        IAnalysisIssueLocation PrimaryLocation { get; }

        string Context { get; }
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
        Vulnerability
    }

    public static class IAnalysisIssueExtensions
    {
        public static bool IsFileLevel(this IAnalysisIssueBase issue)
        {
            return issue.PrimaryLocation.TextRange == null;
        }
    }
}
