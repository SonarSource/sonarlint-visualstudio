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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions
{
    internal sealed class IssueLocationActionsSource : ISuggestedActionsSource
    {
        private readonly IAnalysisIssueSelectionService selectionService;
        private readonly ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator;
        private readonly ITagAggregator<ISelectedIssueLocationTag> selectedIssueLocationsTagAggregator;

        public IssueLocationActionsSource(IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService, ITextBuffer textBuffer, IAnalysisIssueSelectionService selectionService)
        {
            this.selectionService = selectionService;

            issueLocationsTagAggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<IIssueLocationTag>(textBuffer);
            issueLocationsTagAggregator.TagsChanged += TagAggregator_TagsChanged;

            selectedIssueLocationsTagAggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<ISelectedIssueLocationTag>(textBuffer);
            selectedIssueLocationsTagAggregator.TagsChanged += TagAggregator_TagsChanged;
        }

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            var allActions = new List<ISuggestedAction>();

            if (IsOnIssueVisualization(range, out var issueVisualizations))
            {
                var actions = issueVisualizations.Select(x => new SelectIssueVisualizationAction(selectionService, x));
                allActions.AddRange(actions);
            } 

            if (IsOnSelectedVisualization(range))
            {
                allActions.Add(new DeselectIssueVisualizationAction(selectionService));
            }

            if (allActions.Any())
            {
                return new[] {new SuggestedActionSet(allActions)};
            }

            return Enumerable.Empty<SuggestedActionSet>();
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => IsOnIssueVisualization(range, out _) || IsOnSelectedVisualization(range),
                cancellationToken);
        }

        private bool IsOnIssueVisualization(SnapshotSpan range, out IEnumerable<IAnalysisIssueVisualization> issueVisualizationsWithSecondaryLocations)
        {
            var tagSpans = issueLocationsTagAggregator.GetTags(range);

            issueVisualizationsWithSecondaryLocations = tagSpans
                .Select(x => x.Tag.Location)
                .OfType<IAnalysisIssueVisualization>()
                .Where(x=> x.Flows.SelectMany(f => f.Locations).Any());

            return issueVisualizationsWithSecondaryLocations.Any();
        }

        private bool IsOnSelectedVisualization(SnapshotSpan range)
        {
            var tagSpans = selectedIssueLocationsTagAggregator.GetTags(range);

            return tagSpans.Any();
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        public void Dispose()
        {
            issueLocationsTagAggregator.TagsChanged -= TagAggregator_TagsChanged;
            issueLocationsTagAggregator.Dispose();

            selectedIssueLocationsTagAggregator.TagsChanged -= TagAggregator_TagsChanged;
            selectedIssueLocationsTagAggregator.Dispose();
        }

        private void TagAggregator_TagsChanged(object sender, TagsChangedEventArgs e)
        {
            SuggestedActionsChanged?.Invoke(sender, e);
        }
    }
}
