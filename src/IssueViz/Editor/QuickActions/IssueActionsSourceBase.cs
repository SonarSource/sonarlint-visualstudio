/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;

internal abstract class IssueActionsSourceBase : ISuggestedActionsSource
{
    private readonly ILightBulbBroker lightBulbBroker;
    private readonly ITextView textView;
    private readonly IThreadHandling threadHandling;
    private readonly ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator;
    protected readonly ILogger logger;

    protected IssueActionsSourceBase(
        ILightBulbBroker lightBulbBroker,
        IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
        ITextView textView,
        ITextBuffer textBuffer,
        ILogger logger,
        IThreadHandling threadHandling)
    {
        this.lightBulbBroker = lightBulbBroker;
        this.textView = textView;
        this.logger = logger.ForVerboseContext(GetType().Name);
        this.threadHandling = threadHandling;

        issueLocationsTagAggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<IIssueLocationTag>(textBuffer);
        issueLocationsTagAggregator.TagsChanged += TagAggregator_TagsChanged;
    }

    protected abstract SuggestedActionSetPriority Priority { get; }

    protected abstract bool TryGetMatchingIssues(IEnumerable<IAnalysisIssueVisualization> issueVisualizations, out IEnumerable<IAnalysisIssueVisualization> matchingIssues);

    protected abstract IEnumerable<ISuggestedAction> CreateActions(IEnumerable<IAnalysisIssueVisualization> matchingIssues);

    public event EventHandler<EventArgs> SuggestedActionsChanged;

    public IEnumerable<SuggestedActionSet> GetSuggestedActions(
        ISuggestedActionCategorySet requestedActionCategories,
        SnapshotSpan range,
        CancellationToken cancellationToken)
    {
        var allActions = new List<ISuggestedAction>();

        try
        {
            if (TryGetMatchingIssuesFromTags(range, out var matchingIssues))
            {
                allActions.AddRange(CreateActions(matchingIssues));
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.LogVerbose($"Exception getting suggested actions: {ex}");
        }

        return allActions.Any()
            ? [new SuggestedActionSet(allActions, priority: Priority)]
            : [];
    }

    public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
    {
        var hasActions = false;
        try
        {
            await threadHandling.RunOnUIThreadAsync(() => hasActions = TryGetMatchingIssuesFromTags(range, out _));
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.LogVerbose($"Exception checking for suggested actions: {ex}");
        }
        return hasActions;
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
            logger.LogVerbose($"Exception handling TagsChanged event: {ex}");
        }
    }

    private bool TryGetMatchingIssuesFromTags(SnapshotSpan range, out IEnumerable<IAnalysisIssueVisualization> matchingIssues)
    {
        var tagSpans = issueLocationsTagAggregator.GetTags(range);

        var issueVisualizations = tagSpans
            .Select(x => x.Tag.Location)
            .OfType<IAnalysisIssueVisualization>();

        return TryGetMatchingIssues(issueVisualizations, out matchingIssues);
    }
}
