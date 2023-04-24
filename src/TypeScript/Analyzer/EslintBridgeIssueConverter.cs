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
using System.Linq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;
using QuickFix = SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract.QuickFix;
using TextRange = SonarLint.VisualStudio.Core.Analysis.TextRange;

namespace SonarLint.VisualStudio.TypeScript.Analyzer
{
    internal interface IEslintBridgeIssueConverter
    {
        IAnalysisIssue Convert(string filePath, Issue issue);
    }

    internal class EslintBridgeIssueConverter : IEslintBridgeIssueConverter
    {
        private readonly IRulesProvider rulesProvider;

        public EslintBridgeIssueConverter(IRulesProvider rulesProvider)
        {
            this.rulesProvider = rulesProvider;
        }

        public IAnalysisIssue Convert(string filePath, Issue issue)
        {
            var ruleDefinitions = rulesProvider.GetDefinitions();
            var ruleDefinition = ruleDefinitions.Single(x => (x.EslintKey != null && x.EslintKey.Equals(issue.RuleId, StringComparison.OrdinalIgnoreCase)) || (x.StylelintKey != null && x.StylelintKey.Equals(issue.RuleId, StringComparison.OrdinalIgnoreCase)));
            var sonarRuleKey = ruleDefinition.RuleKey;
            ITextRange textRange = null;

            if (issue.Line != 0) // if the line is 0 than it means a file level issue.
            {
                textRange = new TextRange(
                        issue.Line,
                        issue.EndLine, // todo: do we need to handle EndLine=0?
                        issue.Column,
                        issue.EndColumn,
                        null);
            }

            return new AnalysisIssue(
                sonarRuleKey,
                Convert(ruleDefinition.Severity),
                Convert(ruleDefinition.Type),
                primaryLocation: new AnalysisIssueLocation(
                    issue.Message,
                    filePath,
                    textRange: textRange
                    ),
                flows: Convert(filePath, issue.SecondaryLocations),
                fixes: ConvertQuickFixes(issue.QuickFixes));
        }

        private IReadOnlyList<IQuickFix> ConvertQuickFixes(IEnumerable<QuickFix> issueQuickFixes)
        {
            return issueQuickFixes?.Select(x =>
                new Core.Analysis.QuickFix(x.Message,
                    x.Edits.Select(edit => new Core.Analysis.Edit(edit.Text,
                        new Core.Analysis.TextRange(
                            edit.TextRange.Line,
                            edit.TextRange.EndLine,
                            edit.TextRange.Column,
                            edit.TextRange.EndColumn,
                            null))).ToList()
                )).ToList();
        }

        internal static /* for testing */ AnalysisIssueSeverity Convert(RuleSeverity ruleSeverity)
        {
            switch (ruleSeverity)
            {
                case RuleSeverity.BLOCKER:
                    return AnalysisIssueSeverity.Blocker;

                case RuleSeverity.CRITICAL:
                    return AnalysisIssueSeverity.Critical;

                case RuleSeverity.INFO:
                    return AnalysisIssueSeverity.Info;

                case RuleSeverity.MAJOR:
                    return AnalysisIssueSeverity.Major;

                case RuleSeverity.MINOR:
                    return AnalysisIssueSeverity.Minor;

                default:
                    throw new ArgumentOutOfRangeException(nameof(ruleSeverity));
            }
        }

        internal static /* for testing */ AnalysisIssueType Convert(RuleType ruleType)
        {
            switch (ruleType)
            {
                case RuleType.BUG:
                    return AnalysisIssueType.Bug;

                case RuleType.CODE_SMELL:
                    return AnalysisIssueType.CodeSmell;

                case RuleType.VULNERABILITY:
                    return AnalysisIssueType.Vulnerability;

                default:
                    throw new ArgumentOutOfRangeException(nameof(ruleType));
            }
        }

        private IReadOnlyList<IAnalysisIssueFlow> Convert(string filePath, IssueLocation[] issueLocations)
        {
            var locations = issueLocations?.Select(x => Convert(filePath, x));

            return locations == null || !locations.Any()
                ? Array.Empty<IAnalysisIssueFlow>()
                : new[] { new AnalysisIssueFlow(locations.ToArray()) };
        }

        private IAnalysisIssueLocation Convert(string filePath, IssueLocation issueLocation) =>
            new AnalysisIssueLocation(
                issueLocation.Message,
                filePath,
                textRange: new TextRange(
                    issueLocation.Line,
                    issueLocation.EndLine,
                    issueLocation.Column,
                    issueLocation.EndColumn,
                    null));
    }
}
