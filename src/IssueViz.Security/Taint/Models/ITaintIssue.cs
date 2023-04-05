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
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models
{
    internal interface ITaintIssue : IAnalysisIssueBase
    {
        string IssueKey { get; }

        AnalysisIssueSeverity Severity { get; }

        DateTimeOffset CreationTimestamp { get; }

        DateTimeOffset LastUpdateTimestamp { get; }
    }

    internal class TaintIssue : ITaintIssue
    {
        private static readonly IReadOnlyList<IAnalysisIssueFlow> EmptyFlows = Array.Empty<IAnalysisIssueFlow>();

        public TaintIssue(string issueKey,
            string ruleKey,
            IAnalysisIssueLocation primaryLocation,
            AnalysisIssueSeverity severity,
            DateTimeOffset creationTimestamp,
            DateTimeOffset lastUpdateTimestamp,
            IReadOnlyList<IAnalysisIssueFlow> flows,
            string context)
        {
            IssueKey = issueKey;
            RuleKey = ruleKey;
            PrimaryLocation = primaryLocation ?? throw new ArgumentNullException(nameof(primaryLocation));
            Severity = severity;
            CreationTimestamp = creationTimestamp;
            LastUpdateTimestamp = lastUpdateTimestamp;
            Flows = flows ?? EmptyFlows;
            Context = context;
        }

        public string IssueKey { get; }
        public string RuleKey { get; }
        public AnalysisIssueSeverity Severity { get; }
        public DateTimeOffset CreationTimestamp { get; }
        public DateTimeOffset LastUpdateTimestamp { get; }
        public IReadOnlyList<IAnalysisIssueFlow> Flows { get; }
        public IAnalysisIssueLocation PrimaryLocation { get; }
        public string Context { get; }
    }
}
