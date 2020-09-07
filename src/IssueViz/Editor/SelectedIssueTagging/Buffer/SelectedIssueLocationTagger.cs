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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Buffer
{
    internal class SelectedIssueLocationTagger: FilteringTaggerBase<IIssueLocationTag, ISelectedIssueLocationTag>
    {
        private readonly IAnalysisIssueSelectionService issueSelectionService;

        private bool disposedValue;

        public SelectedIssueLocationTagger(ITagAggregator<IIssueLocationTag> tagAggregator, ITextBuffer textBuffer, IAnalysisIssueSelectionService issueSelectionService)
            : base(tagAggregator, textBuffer)
        {
            this.issueSelectionService = issueSelectionService;

            // Changing any of the selected issue/flow/location will always result in the
            // "SelectedLocationChanged" event being raised
            issueSelectionService.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.SelectionChangeLevel == SelectionChangeLevel.Flow || e.SelectionChangeLevel == SelectionChangeLevel.Issue)
            {
                UpdateWholeSpan();
            }
        }

        #region ITagger methods

        protected override TagSpan<ISelectedIssueLocationTag> CreateTagSpan(IIssueLocationTag trackedTag, NormalizedSnapshotSpanCollection spans)
        {
            return new TagSpan<ISelectedIssueLocationTag>(trackedTag.Location.Span.Value, new SelectedIssueLocationTag(trackedTag.Location));
        }

        protected override IEnumerable<IMappingTagSpan<IIssueLocationTag>> Filter(IEnumerable<IMappingTagSpan<IIssueLocationTag>> trackedTagSpans)
        {
            var selectedSecondaryLocVizs = issueSelectionService.SelectedFlow?.Locations;
            if (selectedSecondaryLocVizs == null || selectedSecondaryLocVizs.Count == 0) { return Array.Empty<IMappingTagSpan<IIssueLocationTag>>(); }

            // Filter to any selected locations.
            // There could be multiple secondary issues locations in this file for different issues.
            return trackedTagSpans
                .Where(tagSpan => selectedSecondaryLocVizs.Contains(tagSpan.Tag.Location));
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && !disposedValue)
            {
                issueSelectionService.SelectionChanged -= OnSelectionChanged;
                disposedValue = true;
            }
        }
    }
}
