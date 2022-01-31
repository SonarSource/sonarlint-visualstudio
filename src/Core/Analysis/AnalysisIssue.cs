﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

namespace SonarLint.VisualStudio.Core.Analysis
{
    public class AnalysisIssue : IAnalysisIssue
    {
        private static readonly IReadOnlyList<IAnalysisIssueFlow> EmptyFlows = Array.Empty<IAnalysisIssueFlow>();
        private static readonly IReadOnlyList<IQuickFix> EmptyFixes = Array.Empty<IQuickFix>();

        public AnalysisIssue(
            string ruleKey, AnalysisIssueSeverity severity, AnalysisIssueType type,
            string message, string filePath,
            int startLine, int endLine,
            int startLineOffset, int endLineOffset,
            string lineHash,
            IReadOnlyList<IAnalysisIssueFlow> flows,
            IReadOnlyList<IQuickFix> fixes = null
            )
        {
            RuleKey = ruleKey;
            Severity = severity;
            Type = type;
            StartLine = startLine;
            StartLineOffset = startLineOffset;
            EndLine = endLine;
            EndLineOffset = endLineOffset;
            FilePath = filePath;
            Message = message;
            LineHash = lineHash;
            Flows = flows ?? EmptyFlows;
            Fixes = fixes ?? EmptyFixes;
        }

        public string RuleKey { get; }

        public AnalysisIssueSeverity Severity { get; }

        public AnalysisIssueType Type { get; }

        public int StartLine { get; }

        public int EndLine { get; }

        public int StartLineOffset { get; }

        public int EndLineOffset { get; }

        public string LineHash { get; }

        public string Message { get; }

        public string FilePath { get; }

        public IReadOnlyList<IAnalysisIssueFlow> Flows { get; }

        public IReadOnlyList<IQuickFix> Fixes { get; }
    }

    public class AnalysisIssueFlow : IAnalysisIssueFlow
    {
        public AnalysisIssueFlow(IReadOnlyList<IAnalysisIssueLocation> locations)
        {
            Locations = locations ?? throw new ArgumentNullException(nameof(locations));
        }

        public IReadOnlyList<IAnalysisIssueLocation> Locations { get; }
    }

    public class AnalysisIssueLocation : IAnalysisIssueLocation
    {
        public AnalysisIssueLocation(
            string message, string filePath,
            int startLine, int endLine,
            int startLineOffset, int endLineOffset,
            string lineHash)
        {
            Message = message;
            FilePath = filePath;
            StartLine = startLine;
            EndLine = endLine;
            StartLineOffset = startLineOffset;
            EndLineOffset = endLineOffset;
            LineHash = lineHash;
        }

        public string FilePath { get; }

        public string Message { get; }

        public int StartLine { get; }

        public int EndLine { get; }

        public int StartLineOffset { get; }

        public int EndLineOffset { get; }

        public string LineHash { get; }
    }
}
