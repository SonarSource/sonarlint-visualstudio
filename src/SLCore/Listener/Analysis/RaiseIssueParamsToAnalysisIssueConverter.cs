/*
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;

namespace SonarLint.VisualStudio.SLCore.Listener.Analysis
{
    [Export(typeof(IRaiseIssueParamsToAnalysisIssueConverter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class RaiseIssueParamsToAnalysisIssueConverter : IRaiseIssueParamsToAnalysisIssueConverter
    {
        public IEnumerable<IAnalysisIssue> GetAnalysisIssues(FileUri fileUri, IEnumerable<RaisedIssueDto> raisedIssues) => 
            raisedIssues
                .Select(item => new AnalysisIssue(item.ruleKey,
                    item.severity.ToAnalysisIssueSeverity(),
                    item.type.ToAnalysisIssueType(),
                    GetHighestSoftwareQualitySeverity(item.impacts),
                    GetAnalysisIssueLocation(fileUri.LocalPath, item.primaryMessage, item.textRange),
                    item.flows?.Select(GetAnalysisIssueFlow).ToList(),
                    item.quickFixes?.Select(qf => GetQuickFix(fileUri, qf)).Where(qf => qf is not null).ToList(),
                    item.ruleDescriptionContextKey))
                .ToList();

        private static SoftwareQualitySeverity? GetHighestSoftwareQualitySeverity(List<ImpactDto> impacts) => 
            impacts is not null && impacts.Any() 
                ? impacts.Max(i => i.impactSeverity).ToSoftwareQualitySeverity() 
                : null;

        private static IAnalysisIssueLocation GetAnalysisIssueLocation(string filePath, string message, TextRangeDto textRangeDto) => 
            new AnalysisIssueLocation(message,
                filePath,
                new TextRange(textRangeDto.startLine,
                    textRangeDto.endLine,
                    textRangeDto.startLineOffset,
                    textRangeDto.endLineOffset,
                    null));

        private static IAnalysisIssueFlow GetAnalysisIssueFlow(IssueFlowDto issueFlowDto) =>
            new AnalysisIssueFlow(issueFlowDto.locations.Select(l => GetAnalysisIssueLocation(l.fileUri.LocalPath, l.message, l.textRange)).ToList());

        private static IQuickFix GetQuickFix(FileUri fileURi, QuickFixDto quickFixDto) =>
            quickFixDto.inputFileEdits.Find(e => e.target == fileURi) is { } fileEdit
                ? new QuickFix(quickFixDto.message, fileEdit.textEdits.Select(GetEdit).ToList())
                : null;

        private static IEdit GetEdit(TextEditDto textEdit) => 
            new Edit(textEdit.newText, new TextRange(textEdit.range.startLine, textEdit.range.endLine, textEdit.range.startLineOffset, textEdit.range.endLineOffset, null));
    }
}
