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
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor;

namespace SonarLint.VisualStudio.IssueVisualization.Models
{
    public interface IAnalysisIssueVisualizationConverter
    {
        IAnalysisIssueVisualization Convert(IAnalysisIssueBase issue, ITextSnapshot textSnapshot = null);
    }

    [Export(typeof(IAnalysisIssueVisualizationConverter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    internal class AnalysisIssueVisualizationConverter(IIssueSpanCalculator issueSpanCalculator, ISpanTranslator spanTranslator) : IAnalysisIssueVisualizationConverter
    {
        private static readonly IReadOnlyList<IAnalysisIssueFlowVisualization> EmptyConvertedFlows = [];
        private static readonly IReadOnlyList<IQuickFixApplication> EmptyConvertedFixes = [];

        public IAnalysisIssueVisualization Convert(IAnalysisIssueBase issue, ITextSnapshot textSnapshot = null)
        {
            var issueSpan = textSnapshot == null
                ? null
                : issueSpanCalculator.CalculateSpan(issue.PrimaryLocation.TextRange, textSnapshot);

            var flows = Convert(issue.Flows);

            var quickFixes = GetQuickFixVisualizations(issue, textSnapshot);

            var issueVisualization = new AnalysisIssueVisualization(flows, issue, issueSpan, quickFixes);

            if (textSnapshot != null)
            {
                CalculateSpanForSecondaryLocationsInSameFile(issueVisualization, textSnapshot);
            }

            return issueVisualization;
        }

        private void CalculateSpanForSecondaryLocationsInSameFile(IAnalysisIssueVisualization issueVisualization, ITextSnapshot textSnapshot)
        {
            var locationsInSameFile = issueVisualization
                .Flows
                .SelectMany(x => x.Locations)
                .Where(x => PathHelper.IsMatchingPath(x.CurrentFilePath, issueVisualization.CurrentFilePath));

            foreach (var locationVisualization in locationsInSameFile)
            {
                locationVisualization.Span = issueSpanCalculator.CalculateSpan(locationVisualization.Location.TextRange, textSnapshot);
            }
        }

        private IReadOnlyList<IAnalysisIssueFlowVisualization> Convert(IEnumerable<IAnalysisIssueFlow> flows)
        {
            if (!flows.Any())
            {
                return EmptyConvertedFlows;
            }

            var flowNumber = 1;

            var convertedFlows = flows
                .Select(x => new AnalysisIssueFlowVisualization(flowNumber++, Convert(x.Locations), x))
                .Cast<IAnalysisIssueFlowVisualization>()
                .ToArray();

            return convertedFlows;
        }

        private IReadOnlyList<IAnalysisIssueLocationVisualization> Convert(IEnumerable<IAnalysisIssueLocation> locations)
        {
            var locationNumber = 1;

            var convertedLocations = locations
                .Select(location => new AnalysisIssueLocationVisualization(locationNumber++, location))
                .Cast<IAnalysisIssueLocationVisualization>()
                .ToArray();

            return convertedLocations;
        }

        private IReadOnlyList<IQuickFixApplication> GetQuickFixVisualizations(IAnalysisIssueBase issue, ITextSnapshot textSnapshot)
        {
            if (!(issue is IAnalysisIssue analysisIssue) || textSnapshot == null)
            {
                return EmptyConvertedFixes;
            }

            return analysisIssue
                .Fixes
                .Select(fix =>
                {
                    if (fix is ITextBasedQuickFix textBasedQuickFix)
                    {
                        return HandleTextBasedQuickFix(textSnapshot, textBasedQuickFix);
                    }
                    return null;
                })
                .Where(x => x is not null).ToArray();
        }

        private TextBasedQuickFixApplication HandleTextBasedQuickFix(
            ITextSnapshot textSnapshot,
            ITextBasedQuickFix textBasedQuickFix)
        {
            var editVisualizations = textBasedQuickFix.Edits.Select(edit =>
            {
                var editSpan = issueSpanCalculator.CalculateSpan(edit.RangeToReplace, textSnapshot) ?? new SnapshotSpan();

                return new TextBasedQuickFixEditVisualization(edit, editSpan);
            });

            var fixVisualization = new TextBasedQuickFixVisualization(textBasedQuickFix, editVisualizations.ToArray());

            return new TextBasedQuickFixApplication(fixVisualization, spanTranslator);
        }
    }
}
