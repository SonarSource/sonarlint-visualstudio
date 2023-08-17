/*
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes
{
    internal sealed class QuickFixActionsSource : ISuggestedActionsSource
    {
        private readonly ILightBulbBroker lightBulbBroker;
        private readonly ITextView textView;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;
        private readonly ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator;
        private readonly IQuickFixesTelemetryManager quickFixesTelemetryManager;

        public QuickFixActionsSource(ILightBulbBroker lightBulbBroker,
            IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
            ITextView textView,
            IQuickFixesTelemetryManager quickFixesTelemetryManager,
            ILogger logger)
            : this(lightBulbBroker, bufferTagAggregatorFactoryService, textView, quickFixesTelemetryManager, logger, ThreadHandling.Instance)
        {
        }

        internal QuickFixActionsSource(ILightBulbBroker lightBulbBroker,
            IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
            ITextView textView,
            IQuickFixesTelemetryManager quickFixesTelemetryManager,
            ILogger logger,
            IThreadHandling threadHandling)
        {
            this.lightBulbBroker = lightBulbBroker;
            this.textView = textView;
            this.quickFixesTelemetryManager = quickFixesTelemetryManager;
            this.logger = logger;
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

            try
            {
                if (IsOnIssueWithApplicableQuickFixes(range, out var issuesWithFixes))
                {
                    foreach (var issueViz in issuesWithFixes)
                    {
                        var applicableFixes = issueViz.QuickFixes.Where(x => x.CanBeApplied(textView.TextSnapshot));

                        allActions.AddRange(applicableFixes.Select(fix => new QuickFixSuggestedAction(fix, textView.TextBuffer, issueViz, quickFixesTelemetryManager, logger)));
                    }
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Resources.ERR_QuickFixes_Exception, ex));
            }

            return allActions.Any()
                ? new[] { new SuggestedActionSet(allActions) }
                : Enumerable.Empty<SuggestedActionSet>();
        }

        public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            var hasActions = false;
            try
            {
                await threadHandling.RunOnUIThreadAsync(() => hasActions = IsOnIssueWithApplicableQuickFixes(range, out _));
            }
            catch(Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Resources.ERR_QuickFixes_Exception, ex));
            }
            return hasActions;
        }

        private bool IsOnIssueWithApplicableQuickFixes(SnapshotSpan range, out IEnumerable<IAnalysisIssueVisualization> issuesWithFixes)
        {
            var tagSpans = issueLocationsTagAggregator.GetTags(range);

            issuesWithFixes = tagSpans
                .Select(x => x.Tag.Location)
                .OfType<IAnalysisIssueVisualization>()
                .Where(x =>
                    x.QuickFixes.Any(fix => fix.CanBeApplied(textView.TextSnapshot)));

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

        private void TagAggregator_TagsChanged(object sender, TagsChangedEventArgs e)
            => HandleTagsChangedAsync().Forget();

        internal /* for testing */ async Task HandleTagsChangedAsync()
        {
            try
            {
                await threadHandling.RunOnUIThreadAsync(() => lightBulbBroker.DismissSession(textView));

                SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogVerbose($"[QuickFixActionsSource] Exception handling TagsChanged event: {ex}");
            }
        }
    }
}
