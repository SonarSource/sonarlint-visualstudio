﻿/*
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions
{
    internal sealed class IssueLocationActionsSource : ISuggestedActionsSource
    {
        private readonly ILightBulbBroker lightBulbBroker;
        private readonly IVsUIShell vsUiShell;
        private readonly ITextView textView;
        private readonly IIssueSelectionService selectionService;
        private readonly ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator;
        private readonly ITagAggregator<ISelectedIssueLocationTag> selectedIssueLocationsTagAggregator;

        public IssueLocationActionsSource(ILightBulbBroker lightBulbBroker, 
            IVsUIShell vsUiShell, 
            IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService, 
            ITextView textView,
            IIssueSelectionService selectionService)
        {
            this.lightBulbBroker = lightBulbBroker;
            this.vsUiShell = vsUiShell;
            this.textView = textView;
            this.selectionService = selectionService;

            issueLocationsTagAggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<IIssueLocationTag>(textView.TextBuffer);
            issueLocationsTagAggregator.TagsChanged += TagAggregator_TagsChanged;

            selectedIssueLocationsTagAggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<ISelectedIssueLocationTag>(textView.TextBuffer);
            selectedIssueLocationsTagAggregator.TagsChanged += TagAggregator_TagsChanged;
        }

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        /// <summary>
        /// Method returns a "show" lightbulb for each issue in the range that has secondary locations
        /// and a single "hide" lightbulb if the range contains any selected locations
        /// </summary>
        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            var allActions = new List<ISuggestedAction>();

            if (IsOnIssueWithSecondaryLocations(range, out var issueVisualizations))
            {
                var actions = issueVisualizations.Select(x => new SelectIssueVisualizationAction(vsUiShell, selectionService, x));
                allActions.AddRange(actions);
            } 

            if (IsOnSelectedVisualizationWithSecondaries(range))
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
            return Task.Factory.StartNew(() => IsOnIssueWithSecondaryLocations(range, out _) || IsOnSelectedVisualizationWithSecondaries(range),
                cancellationToken);
        }

        private bool IsOnIssueWithSecondaryLocations(SnapshotSpan range, out IEnumerable<IAnalysisIssueVisualization> issuesWithSecondaryLocations)
        {
            var tagSpans = issueLocationsTagAggregator.GetTags(range);

            issuesWithSecondaryLocations = tagSpans
                .Select(x => x.Tag.Location)
                .OfType<IAnalysisIssueVisualization>()
                .Where(x => x.Flows.SelectMany(f => f.Locations).Any());

            return issuesWithSecondaryLocations.Any();
        }

        private bool IsOnSelectedVisualizationWithSecondaries(SnapshotSpan range)
        {
            var primaryLocationsTagSpans = issueLocationsTagAggregator.GetTags(range);
            var isOnSelectedPrimaryLocation = primaryLocationsTagSpans.Select(x => x.Tag.Location)
                .OfType<IAnalysisIssueVisualization>()
                .Any(x => x == selectionService.SelectedIssue && x.GetSecondaryLocations().Any());

            var selectedLocationsTagSpans = selectedIssueLocationsTagAggregator.GetTags(range);
            var isOnSelectedSecondaryLocation = selectedLocationsTagSpans.Any();

            return isOnSelectedPrimaryLocation || isOnSelectedSecondaryLocation;
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
            RunOnUIThread.Run(() => lightBulbBroker.DismissSession(textView));

            SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
