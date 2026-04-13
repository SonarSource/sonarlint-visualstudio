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
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;

internal sealed class QuickFixActionsSource(
    ILightBulbBroker lightBulbBroker,
    IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
    ITextView textView,
    ITextBuffer textBuffer,
    IQuickFixesTelemetryManager quickFixesTelemetryManager,
    IMessageBox messageBox,
    ILogger logger,
    IThreadHandling threadHandling)
    : IssueActionsSourceBase(lightBulbBroker, bufferTagAggregatorFactoryService, textView, textBuffer, logger, threadHandling)
{
    private readonly ITextBuffer textBuffer = textBuffer;
    private readonly IThreadHandling threadHandling = threadHandling;

    protected override SuggestedActionSetPriority Priority => SuggestedActionSetPriority.Medium;

    protected override bool TryGetMatchingIssues(IEnumerable<IAnalysisIssueVisualization> issueVisualizations, out IEnumerable<IAnalysisIssueVisualization> matchingIssues)
    {
        matchingIssues = issueVisualizations
            .Where(x => x.QuickFixes.Any(fix => fix.CanBeApplied(textBuffer.CurrentSnapshot)));

        return matchingIssues.Any();
    }

    protected override IEnumerable<ISuggestedAction> CreateActions(IEnumerable<IAnalysisIssueVisualization> matchingIssues) =>
        matchingIssues.SelectMany(issueViz =>
            issueViz.QuickFixes
                .Where(x => x.CanBeApplied(textBuffer.CurrentSnapshot))
                .Select(fix => new QuickFixSuggestedAction(fix, textBuffer, issueViz, quickFixesTelemetryManager, messageBox, Logger, threadHandling)));
}
