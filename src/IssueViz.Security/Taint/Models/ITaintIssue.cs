﻿/*
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

using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models
{
    public interface ITaintIssue : IAnalysisIssueBase
    {
        string IssueKey { get; }

        AnalysisIssueSeverity? Severity { get; }

        SoftwareQualitySeverity? HighestSoftwareQualitySeverity { get; }

        DateTimeOffset CreationTimestamp { get; }
    }

    public class TaintIssue : ITaintIssue
    {
        private static readonly IReadOnlyList<IAnalysisIssueFlow> EmptyFlows = Array.Empty<IAnalysisIssueFlow>();

        public TaintIssue(string issueKey,
            string ruleKey,
            IAnalysisIssueLocation primaryLocation,
            AnalysisIssueSeverity? severity,
            SoftwareQualitySeverity? highestSoftwareQualitySeverity,
            DateTimeOffset creationTimestamp,
            IReadOnlyList<IAnalysisIssueFlow> flows,
            string ruleDescriptionContextKey)
        {
            IssueKey = issueKey;
            RuleKey = ruleKey;
            PrimaryLocation = primaryLocation ?? throw new ArgumentNullException(nameof(primaryLocation));
            Severity = severity;
            CreationTimestamp = creationTimestamp;
            Flows = flows ?? EmptyFlows;
            RuleDescriptionContextKey = ruleDescriptionContextKey;
            HighestSoftwareQualitySeverity = highestSoftwareQualitySeverity;

            if (!severity.HasValue && !highestSoftwareQualitySeverity.HasValue)
            {
                throw new ArgumentException(string.Format(TaintResources.TaintIssue_SeverityUndefined, IssueKey));
            }
        }

        public string IssueKey { get; }
        public string RuleKey { get; }
        public AnalysisIssueSeverity? Severity { get; }
        public SoftwareQualitySeverity? HighestSoftwareQualitySeverity { get; }
        public DateTimeOffset CreationTimestamp { get; }
        public IReadOnlyList<IAnalysisIssueFlow> Flows { get; }
        public IAnalysisIssueLocation PrimaryLocation { get; }
        public string RuleDescriptionContextKey { get; }
    }
}
