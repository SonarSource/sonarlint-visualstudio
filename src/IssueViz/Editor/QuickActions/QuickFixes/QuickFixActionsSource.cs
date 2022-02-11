/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes
{
    internal sealed class QuickFixActionsSource : ISuggestedActionsSource
    {
        private readonly ILightBulbBroker lightBulbBroker;
        private readonly ITextView textView;
        private readonly IThreadHandling threadHandling;
        private readonly ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator;

        public QuickFixActionsSource(ILightBulbBroker lightBulbBroker,
            IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
            ITextView textView)
            : this(lightBulbBroker, bufferTagAggregatorFactoryService, textView, new ThreadHandling())
        {
        }

        internal QuickFixActionsSource(ILightBulbBroker lightBulbBroker,
            IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
            ITextView textView,
            IThreadHandling threadHandling)
        {
            this.lightBulbBroker = lightBulbBroker;
            this.textView = textView;
            this.threadHandling = threadHandling;

            issueLocationsTagAggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<IIssueLocationTag>(textView.TextBuffer);
            issueLocationsTagAggregator.TagsChanged += TagAggregator_TagsChanged;
        }

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(
            ISuggestedActionCategorySet requestedActionCategories, 
            SnapshotSpan range,
            CancellationToken cancellationToken)
        {
            var allActions = new List<ISuggestedAction>();

            if (IsOnIssueWithQuickFixes(range, out var issuesWithFixes))
            {
                foreach (var issueViz in issuesWithFixes)
                {
                    allActions.AddRange(issueViz.QuickFixes.Select(fix => new QuickFixSuggestedAction(fix, textView.TextBuffer, issueViz)));
                }
            }

            return allActions.Any()
                ? new[] { new SuggestedActionSet(allActions) }
                : Enumerable.Empty<SuggestedActionSet>();
        }

        public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            var hasActions = false;

            await threadHandling.RunOnUIThread(() => hasActions = IsOnIssueWithQuickFixes(range, out _));

            return hasActions;
        }

        private bool IsOnIssueWithQuickFixes(SnapshotSpan range, out IEnumerable<IAnalysisIssueVisualization> issuesWithFixes)
        {
            var tagSpans = issueLocationsTagAggregator.GetTags(range);

            issuesWithFixes = tagSpans
                .Select(x => x.Tag.Location)
                .OfType<IAnalysisIssueVisualization>()
                .Where(x => x.QuickFixes.Any());

            return issuesWithFixes.Any();
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
        }

        private async void TagAggregator_TagsChanged(object sender, TagsChangedEventArgs e)
        {
            await threadHandling.RunOnUIThread(() => lightBulbBroker.DismissSession(textView));

            SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
