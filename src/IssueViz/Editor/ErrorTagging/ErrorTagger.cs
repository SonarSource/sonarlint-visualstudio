/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging;

internal class ErrorTagger : FilteringTaggerBase<IIssueLocationTag, IErrorTag>
{
    private readonly IErrorTagTooltipProvider errorTagTooltipProvider;
    private readonly IFocusOnNewCodeService focusOnNewCodeService;

    public ErrorTagger(ITagAggregator<IIssueLocationTag> tagAggregator, ITextBuffer textBuffer, IErrorTagTooltipProvider errorTagTooltipProvider, IFocusOnNewCodeService focusOnNewCodeService)
        : base(tagAggregator, textBuffer)
    {
        this.errorTagTooltipProvider = errorTagTooltipProvider;
        this.focusOnNewCodeService = focusOnNewCodeService;
        focusOnNewCodeService.Changed += FocusOnNewCodeServiceOnChanged;
    }

    private void FocusOnNewCodeServiceOnChanged(object sender, NewCodeStatusChangedEventArgs e) => NotifyTagsChanged();

    protected override TagSpan<IErrorTag> CreateTagSpan(IIssueLocationTag trackedTag, NormalizedSnapshotSpanCollection spans)
    {
        var issueViz = (IAnalysisIssueVisualization) trackedTag.Location;
        var errorType = focusOnNewCodeService.Current.IsEnabled && !issueViz.IsOnNewCode ? PredefinedErrorTypeNames.HintedSuggestion : PredefinedErrorTypeNames.Warning;
        return new TagSpan<IErrorTag>(trackedTag.Location.Span.Value, new SonarErrorTag(errorType, issueViz.Issue, errorTagTooltipProvider));
    }

    protected override IEnumerable<IMappingTagSpan<IIssueLocationTag>> Filter(IEnumerable<IMappingTagSpan<IIssueLocationTag>> trackedTagSpans) =>
        trackedTagSpans.Where(x => x.Tag.Location is IAnalysisIssueVisualization issueViz && !issueViz.IsResolved && IsValidPrimaryLocation(issueViz));

    private static bool IsValidPrimaryLocation(IAnalysisIssueVisualization issueViz) => issueViz.Span.IsNavigable();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            focusOnNewCodeService.Changed -= FocusOnNewCodeServiceOnChanged;
        }

        base.Dispose(disposing);
    }
}
