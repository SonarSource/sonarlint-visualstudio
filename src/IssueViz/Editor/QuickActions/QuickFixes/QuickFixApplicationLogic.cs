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

using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;

internal interface IQuickFixApplicationLogic
{
    bool CanBeApplied(IQuickFixApplication quickFix, ITextSnapshot textSnapshot);

    Task<bool> ApplyAsync(IQuickFixApplication quickFix, ITextSnapshot textSnapshot,
        IAnalysisIssueVisualization issueViz, CancellationToken cancellationToken);
}

[Export(typeof(IQuickFixApplicationLogic))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class QuickFixApplicationLogic : IQuickFixApplicationLogic
{
    private readonly IQuickFixesTelemetryManager quickFixesTelemetryManager;
    private readonly IMessageBox messageBox;
    private readonly ILogger logger;

    [ImportingConstructor]
    public QuickFixApplicationLogic(
        IQuickFixesTelemetryManager quickFixesTelemetryManager,
        IMessageBox messageBox,
        ILogger logger)
    {
        this.quickFixesTelemetryManager = quickFixesTelemetryManager;
        this.messageBox = messageBox;
        this.logger = logger.ForContext(Resources.QuickFixSuggestedAction_LogContext);
    }

    public bool CanBeApplied(IQuickFixApplication quickFix, ITextSnapshot textSnapshot)
    {
        return quickFix.CanBeApplied(textSnapshot);
    }

    public async Task<bool> ApplyAsync(IQuickFixApplication quickFix, ITextSnapshot textSnapshot,
        IAnalysisIssueVisualization issueViz, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        if (!quickFix.CanBeApplied(textSnapshot))
        {
            logger.LogVerbose("Quick fix cannot be applied as the text has changed. Issue: " + issueViz.Issue.RuleKey);
            return false;
        }

        var originalSpan = issueViz.Span;
        issueViz.InvalidateSpan();

        var isApplied = false;

        try
        {
            isApplied = await quickFix.ApplyAsync(textSnapshot, cancellationToken);
        }
        finally
        {
            if (!isApplied)
            {
                issueViz.Span = originalSpan;
                NotifyUser(issueViz.Issue.RuleKey);
            }
        }

        if (isApplied)
        {
            quickFixesTelemetryManager.QuickFixApplied(issueViz.Issue.RuleKey);
        }

        return isApplied;
    }

    private void NotifyUser(string ruleId)
    {
        logger.WriteLine(Resources.QuickFixSuggestedAction_CouldNotApply, ruleId);
        messageBox.Show(
            string.Format(Resources.QuickFixSuggestedAction_CouldNotApply, ruleId),
            Resources.QuickFixSuggestedAction_CouldNotApplyMessageBoxCaption,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
