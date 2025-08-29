/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;

namespace SonarLint.VisualStudio.SLCore.Listener.Analysis
{
    [Export(typeof(IRaiseFindingToAnalysisIssueConverter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    public class RaiseFindingToAnalysisIssueConverter(ILogger logger) : IRaiseFindingToAnalysisIssueConverter
    {
        private readonly ILogger logger = logger.ForContext(nameof(RaiseFindingToAnalysisIssueConverter));

        public IEnumerable<IAnalysisIssue> GetAnalysisIssues<T>(FileUri fileUri, IEnumerable<T> raisedFindings) where T : RaisedFindingDto =>
            raisedFindings
                .Select(item => TryCreateAnalysisIssue(fileUri, item))
                .Where(x => x != null)
                .ToList()!;

        private AnalysisIssue? TryCreateAnalysisIssue<T>(FileUri fileUri, T item) where T : RaisedFindingDto
        {
            try
            {
                var id = item.id;
                var itemRuleKey = item.ruleKey;
                var itemIssueServerKey = item.serverKey;
                var isResolved = item.resolved;
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
                        itemIssueServerKey,
                        isResolved,
                        analysisIssueSeverity,
                        analysisIssueType,
                        highestSoftwareQualitySeverity,
                        analysisIssueLocation,
                        analysisIssueFlows,
                        raisedHotspotDto.status.ToHotspotStatus(),
                        readOnlyList,
                        raisedHotspotDto.vulnerabilityProbability.GetHotspotPriority());
                }

                return new AnalysisIssue(id,
                    itemRuleKey,
                    itemIssueServerKey,
                    isResolved,
                    analysisIssueSeverity,
                    analysisIssueType,
                    highestSoftwareQualitySeverity,
                    analysisIssueLocation,
                    analysisIssueFlows,
                    readOnlyList);
            }
            catch (Exception exception)
            {
                logger.WriteLine(SLCoreStrings.RaiseFindingToAnalysisIssueConverter_CreateAnalysisIssueFailed, item?.ruleKey, exception);
                return null;
            }
        }

        private static Impact? GetHighestImpact(List<ImpactDto>? impacts)
        {
            if (impacts is null || impacts.Count == 0)
            {
                return null;
            }
            return impacts.OrderByDescending(i => i.impactSeverity).ThenByDescending(i => i.softwareQuality).First().ToImpact();
        }

        private static IAnalysisIssueLocation GetAnalysisIssueLocation(string filePath, string message, TextRangeDto textRangeDto) =>
            new AnalysisIssueLocation(message, filePath, CopyTextRange(textRangeDto));

        private static TextRange? CopyTextRange(TextRangeDto? textRangeDto)
        {
            if (textRangeDto is null)
            {
                return null;
            }
            return new TextRange(textRangeDto.startLine,
                textRangeDto.endLine,
                textRangeDto.startLineOffset,
                textRangeDto.endLineOffset,
                null);
        }

        private static IAnalysisIssueFlow[] GetFlows(List<IssueFlowDto>? issueFlows)
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

        private static IQuickFixBase? GetQuickFix(FileUri fileURi, QuickFixDto quickFixDto)
        {
            if (quickFixDto.message.StartsWith(RoslynQuickFix.StoragePrefix))
            {
                return HandleRoslynQuickFix(quickFixDto);
            }

            var fileEdits = quickFixDto.inputFileEdits.FindAll(e => e.target == fileURi);
            if (fileEdits.Count == 0)
            {
                return null;
            }

            var textEdits = fileEdits.SelectMany(x => x.textEdits).Select(GetEdit).ToList();
            return new TextBasedQuickFix(quickFixDto.message, textEdits);
        }

        private static IQuickFixBase? HandleRoslynQuickFix(QuickFixDto quickFixDto) =>
            Guid.TryParse(quickFixDto.message.Substring(RoslynQuickFix.StoragePrefix.Length), out var quickFixId)
                ? new RoslynQuickFix(quickFixId)
                : null;

        private static IEdit GetEdit(TextEditDto textEdit) =>
            new Edit(textEdit.newText,
                new TextRange(textEdit.range.startLine, textEdit.range.endLine, textEdit.range.startLineOffset, textEdit.range.endLineOffset, null));
    }
}
