/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes
{
    internal sealed class IssueFixActionsSource : ISuggestedActionsSource
    {
        private readonly ILightBulbBroker lightBulbBroker;
        private readonly IIssueSpanCalculator spanCalculator;
        private readonly ITextView textView;
        private readonly ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator;
        private readonly ITagAggregator<ISelectedIssueLocationTag> selectedIssueLocationsTagAggregator;

        public IssueFixActionsSource(ILightBulbBroker lightBulbBroker, 
            IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService, 
            IIssueSpanCalculator spanCalculator,
            ITextView textView)
        {
            this.lightBulbBroker = lightBulbBroker;
            this.spanCalculator = spanCalculator;
            this.textView = textView;

            issueLocationsTagAggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<IIssueLocationTag>(textView.TextBuffer);
            issueLocationsTagAggregator.TagsChanged += TagAggregator_TagsChanged;

            selectedIssueLocationsTagAggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<ISelectedIssueLocationTag>(textView.TextBuffer);
            selectedIssueLocationsTagAggregator.TagsChanged += TagAggregator_TagsChanged;
        }

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            var allActions = new List<ISuggestedAction>();

            if (IsOnIssueWithQuickFixes(range, out var issuesWithFixes))
            {
                foreach (var issueViz in issuesWithFixes)
                {
                    foreach (var fix in (issueViz.Issue as IAnalysisIssue).Fixes)
                    {
                        var action = new QuickFixSuggestedAction(spanCalculator,
                            textView.TextBuffer.CurrentSnapshot,
                            range,
                            fix);

                        allActions.Add(action);
                    }
                }
            }

            if (allActions.Any())
            {
                return new[] { new SuggestedActionSet(allActions) };
            }

            return Enumerable.Empty<SuggestedActionSet>();
        }
        
        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken) => 
            Task.Factory.StartNew(() => IsOnIssueWithQuickFixes(range, out _), cancellationToken);

        private bool IsOnIssueWithQuickFixes(SnapshotSpan range, out IEnumerable<IAnalysisIssueVisualization> fixes)
        {
            var tagSpans = issueLocationsTagAggregator.GetTags(range);

            fixes = tagSpans
                .Select(x => x.Tag.Location)
                .OfType<IAnalysisIssueVisualization>()
                .Where(x => x.Issue is IAnalysisIssue analysisIssue && analysisIssue.Fixes.Any());

            return fixes.Any();
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
