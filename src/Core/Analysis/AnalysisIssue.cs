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

namespace SonarLint.VisualStudio.Core.Analysis
{
    public class AnalysisIssue : IAnalysisIssue
    {
        private static readonly IReadOnlyList<IAnalysisIssueFlow> EmptyFlows = Array.Empty<IAnalysisIssueFlow>();
        private static readonly IReadOnlyList<IQuickFix> EmptyFixes = Array.Empty<IQuickFix>();

        public AnalysisIssue(
            string ruleKey,
            AnalysisIssueSeverity severity,
            AnalysisIssueType type,
            SoftwareQualitySeverity? highestSoftwareQualitySeverity, 
            IAnalysisIssueLocation primaryLocation,
            IReadOnlyList<IAnalysisIssueFlow> flows,
            IReadOnlyList<IQuickFix> fixes = null,
            string context = null
            )
        {
            RuleKey = ruleKey;
            Severity = severity;
            HighestSoftwareQualitySeverity = highestSoftwareQualitySeverity;
            Type = type;
            PrimaryLocation = primaryLocation ?? throw new ArgumentNullException(nameof(primaryLocation));
            Flows = flows ?? EmptyFlows;
            Fixes = fixes ?? EmptyFixes;
            RuleDescriptionContextKey = context;
        }

        public string RuleKey { get; }

        public AnalysisIssueSeverity Severity { get; }
        
        public SoftwareQualitySeverity? HighestSoftwareQualitySeverity { get; }

        public AnalysisIssueType Type { get; }

        public IReadOnlyList<IAnalysisIssueFlow> Flows { get; }

        public IAnalysisIssueLocation PrimaryLocation { get; }

        public IReadOnlyList<IQuickFix> Fixes { get; }

        public string RuleDescriptionContextKey { get; }
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
        public AnalysisIssueLocation(string message, string filePath, ITextRange textRange)
        {
            Message = message;
            FilePath = filePath;
            TextRange = textRange;
        }

        public string FilePath { get; }

        public string Message { get; }

        public ITextRange TextRange { get; }
    }

    public class TextRange : ITextRange
    {
        public TextRange(int startLine, int endLine, int startLineOffset, int endLineOffset, string lineHash)
        {
            StartLine = startLine;
            EndLine = endLine;
            StartLineOffset = startLineOffset;
            EndLineOffset = endLineOffset;
            LineHash = lineHash;
        }

        public int StartLine { get; }
        public int EndLine { get; }
        public int StartLineOffset { get; }
        public int EndLineOffset { get; }
        public string LineHash { get; }
    }
}
