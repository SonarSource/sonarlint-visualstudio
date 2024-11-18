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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;

namespace SonarLint.VisualStudio.SLCore.Listener.Analysis
{
    [Export(typeof(IRaiseFindingToAnalysisIssueConverter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class RaiseFindingToAnalysisIssueConverter : IRaiseFindingToAnalysisIssueConverter
    {
        public IEnumerable<IAnalysisIssue> GetAnalysisIssues<T>(FileUri fileUri, IEnumerable<T> raisedFindings) where T : RaisedFindingDto =>
            raisedFindings
                .Select(item => CreateAnalysisIssue(fileUri, item))
                .ToList();

        private static AnalysisIssue CreateAnalysisIssue<T>(FileUri fileUri, T item) where T : RaisedFindingDto
        {
            var id = item.id;
            var itemRuleKey = item.ruleKey;
            var analysisIssueSeverity = item.severityMode.Left?.severity.ToAnalysisIssueSeverity();
            var analysisIssueType = item.severityMode.Left?.type.ToAnalysisIssueType();
            var highestSoftwareQualitySeverity = GetHighestImpact(item.severityMode.Right?.impacts);
            var analysisIssueLocation = GetAnalysisIssueLocation(fileUri.LocalPath, item.primaryMessage, item.textRange);
            var analysisIssueFlows = GetFlows(item.flows);
            var readOnlyList = item.quickFixes?.Select(qf => GetQuickFix(fileUri, qf)).Where(qf => qf is not null).ToList();

            if (item is RaisedHotspotDto raisedHotspotDto)
            {
                return new AnalysisHotspotIssue(id,
                    itemRuleKey,
                    analysisIssueSeverity,
                    analysisIssueType,
                    highestSoftwareQualitySeverity,
                    analysisIssueLocation,
                    analysisIssueFlows,
                    readOnlyList,
                    raisedHotspotDto.vulnerabilityProbability.GetHotspotPriority());
            }

            return new AnalysisIssue(id,
                itemRuleKey,
                analysisIssueSeverity,
                analysisIssueType,
                highestSoftwareQualitySeverity,
                analysisIssueLocation,
                analysisIssueFlows,
                readOnlyList);
        }

        private static Impact GetHighestImpact(List<ImpactDto> impacts)
        {
            if(impacts is null || impacts.Count == 0)
            {
                return null;
            }
            return impacts.OrderByDescending(i => i.impactSeverity).ThenByDescending(i => i.softwareQuality).First().ToImpact();
        }

        private static IAnalysisIssueLocation GetAnalysisIssueLocation(string filePath, string message, TextRangeDto textRangeDto) =>
            new AnalysisIssueLocation(message,
                filePath,
                new TextRange(textRangeDto.startLine,
                    textRangeDto.endLine,
                    textRangeDto.startLineOffset,
                    textRangeDto.endLineOffset,
                    null));

        private static IAnalysisIssueFlow[] GetFlows(List<IssueFlowDto> issueFlows)
        {
            if (issueFlows is null || issueFlows.Count == 0)
            {
                return [];
            }

            if (issueFlows.TrueForAll(x => x.locations?.Count == 1))
            {
                return [GetAnalysisIssueFlow(issueFlows.SelectMany(x => x.locations))];
            }

            return issueFlows.Select(x => GetAnalysisIssueFlow(x.locations)).ToArray();
        }

        private static IAnalysisIssueFlow GetAnalysisIssueFlow(IEnumerable<IssueLocationDto> flowLocations) =>
            new AnalysisIssueFlow(flowLocations.Select(l => GetAnalysisIssueLocation(l.fileUri.LocalPath, l.message, l.textRange)).ToList());

        private static IQuickFix GetQuickFix(FileUri fileURi, QuickFixDto quickFixDto) =>
            quickFixDto.inputFileEdits.Find(e => e.target == fileURi) is { } fileEdit
                ? new QuickFix(quickFixDto.message, fileEdit.textEdits.Select(GetEdit).ToList())
                : null;

        private static IEdit GetEdit(TextEditDto textEdit) =>
            new Edit(textEdit.newText,
                new TextRange(textEdit.range.startLine, textEdit.range.endLine, textEdit.range.startLineOffset, textEdit.range.endLineOffset, null));
    }
}
