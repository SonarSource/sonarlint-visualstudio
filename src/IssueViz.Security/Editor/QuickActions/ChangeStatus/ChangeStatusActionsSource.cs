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
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Editor.QuickActions.ChangeStatus;

internal sealed class ChangeStatusActionsSource(
    ILightBulbBroker lightBulbBroker,
    IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
    ITextView textView,
    ITextBuffer textBuffer,
    IMuteIssuesService muteIssuesService,
    ILogger logger,
    IThreadHandling threadHandling)
    : IssueActionsSourceBase(lightBulbBroker, bufferTagAggregatorFactoryService, textView, textBuffer, logger, threadHandling)
{
    protected override SuggestedActionSetPriority Priority => SuggestedActionSetPriority.Low;

    protected override bool TryGetMatchingIssues(IEnumerable<IAnalysisIssueVisualization> issueVisualizations, out IEnumerable<IAnalysisIssueVisualization> matchingIssues)
    {
        matchingIssues = issueVisualizations
            .Where(x => !x.IsResolved && x.IssueServerKey != null);

        return matchingIssues.Any();
    }

    protected override IEnumerable<ISuggestedAction> CreateActions(IEnumerable<IAnalysisIssueVisualization> matchingIssues) =>
        matchingIssues.Select(issueViz => new ChangeStatusSuggestedAction(issueViz, muteIssuesService));
}
