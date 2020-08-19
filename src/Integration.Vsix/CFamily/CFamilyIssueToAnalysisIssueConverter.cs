/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal interface ICFamilyIssueToAnalysisIssueConverter
    {
        IAnalysisIssue Convert(Message message, string sqLanguage, ICFamilyRulesConfig rulesConfiguration);
    }

    [Export(typeof(ICFamilyIssueToAnalysisIssueConverter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CFamilyIssueToAnalysisIssueConverter : ICFamilyIssueToAnalysisIssueConverter
    {
        public IAnalysisIssue Convert(Message cFamilyIssue, string sqLanguage, ICFamilyRulesConfig rulesConfiguration)
        {
            // Lines and character positions are 1-based
            Debug.Assert(cFamilyIssue.Line > 0);

            // BUT special case of EndLine=0, Column=0, EndColumn=0 meaning "select the whole line"
            Debug.Assert(cFamilyIssue.EndLine >= 0);
            Debug.Assert(cFamilyIssue.Column > 0 || cFamilyIssue.Column == 0);
            Debug.Assert(cFamilyIssue.EndColumn > 0 || cFamilyIssue.EndLine == 0);

            // Look up default severity and type
            var defaultSeverity = rulesConfiguration.RulesMetadata[cFamilyIssue.RuleKey].DefaultSeverity;
            var defaultType = rulesConfiguration.RulesMetadata[cFamilyIssue.RuleKey].Type;

            var locations = cFamilyIssue.Parts
                .Select(ToAnalysisIssueLocation)
                .Reverse()
                .ToArray();

            var flows = locations.Any() ? new[] { new AnalysisIssueFlow(locations) } : null;

            return ToAnalysisIssue(cFamilyIssue, sqLanguage, defaultSeverity, defaultType, flows);
        }

        private static IAnalysisIssue ToAnalysisIssue(Message cFamilyIssue, string sqLanguage, IssueSeverity defaultSeverity,
            IssueType defaultType, AnalysisIssueFlow[] flows)
        {
            return new AnalysisIssue
            (
                ruleKey: sqLanguage + ":" + cFamilyIssue.RuleKey,
                severity: Convert(defaultSeverity),
                type: Convert(defaultType),

                filePath: cFamilyIssue.Filename,
                message: cFamilyIssue.Text,
                startLine: cFamilyIssue.Line,
                endLine: cFamilyIssue.EndLine,

                // We don't care about the columns in the special case EndLine=0
                startLineOffset: cFamilyIssue.EndLine == 0 ? 0 : cFamilyIssue.Column - 1,
                endLineOffset: cFamilyIssue.EndLine == 0 ? 0 : cFamilyIssue.EndColumn - 1,

                flows: flows
            );
        }

        private static AnalysisIssueLocation ToAnalysisIssueLocation(MessagePart cFamilyIssueLocation)
        {
            return new AnalysisIssueLocation
            (
                filePath: cFamilyIssueLocation.Filename,
                message: cFamilyIssueLocation.Text,
                startLine: cFamilyIssueLocation.Line,
                endLine: cFamilyIssueLocation.EndLine,

                // We don't care about the columns in the special case EndLine=0
                startLineOffset: cFamilyIssueLocation.EndLine == 0 ? 0 : cFamilyIssueLocation.Column - 1,
                endLineOffset: cFamilyIssueLocation.EndLine == 0 ? 0 : cFamilyIssueLocation.EndColumn - 1
            );
        }

        /// <summary>
        /// Converts from the CFamily issue severity enum to the standard AnalysisIssueSeverity
        /// </summary>
        internal /* for testing */ static AnalysisIssueSeverity Convert(IssueSeverity issueSeverity)
        {
            switch (issueSeverity)
            {
                case IssueSeverity.Blocker:
                    return AnalysisIssueSeverity.Blocker;
                case IssueSeverity.Critical:
                    return AnalysisIssueSeverity.Critical;
                case IssueSeverity.Info:
                    return AnalysisIssueSeverity.Info;
                case IssueSeverity.Major:
                    return AnalysisIssueSeverity.Major;
                case IssueSeverity.Minor:
                    return AnalysisIssueSeverity.Minor;

                default:
                    throw new ArgumentOutOfRangeException(nameof(issueSeverity));
            }
        }

        /// <summary>
        /// Converts from the CFamily issue type enum to the standard AnalysisIssueType
        /// </summary>
        internal /* for testing */static AnalysisIssueType Convert(IssueType issueType)
        {
            switch (issueType)
            {
                case IssueType.Bug:
                    return AnalysisIssueType.Bug;
                case IssueType.CodeSmell:
                    return AnalysisIssueType.CodeSmell;
                case IssueType.Vulnerability:
                    return AnalysisIssueType.Vulnerability;

                default:
                    throw new ArgumentOutOfRangeException(nameof(issueType));
            }
        }
    }
}
