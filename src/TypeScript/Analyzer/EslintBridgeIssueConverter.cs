/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Linq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.Analyzer
{
    internal interface IEslintBridgeIssueConverter
    {
        IAnalysisIssue Convert(string filePath, Issue issue);
    }

    internal class EslintBridgeIssueConverter : IEslintBridgeIssueConverter
    {
        public IAnalysisIssue Convert(string filePath, Issue issue)
        {
            // todo: get values from rule configuration
            var ruleSeverity = AnalysisIssueSeverity.Info;
            var ruleType = AnalysisIssueType.Vulnerability;

            // todo: calculate line hash
            string lineHash = null;

            return new AnalysisIssue(
                issue.RuleId,
                ruleSeverity,
                ruleType,
                issue.Message,
                filePath,
                issue.Line,
                issue.EndLine,
                issue.Column,
                issue.EndColumn,
                lineHash,
                Convert(filePath, issue.SecondaryLocations));
        }

        private IReadOnlyList<IAnalysisIssueFlow> Convert(string filePath, IssueLocation[] issueLocations)
        {
            var locations = issueLocations?.Select(x => Convert(filePath, x));

            return locations == null || !locations.Any()
                ? Array.Empty<IAnalysisIssueFlow>()
                : new[] {new AnalysisIssueFlow(locations.ToArray())};
        }

        private IAnalysisIssueLocation Convert(string filePath, IssueLocation issueLocation)
        {
            // todo: calculate line hash
            string lineHash = null;

            return new AnalysisIssueLocation(
                issueLocation.Message,
                filePath,
                issueLocation.Line,
                issueLocation.EndLine,
                issueLocation.Column,
                issueLocation.EndColumn,
                lineHash);
        }
    }
}
