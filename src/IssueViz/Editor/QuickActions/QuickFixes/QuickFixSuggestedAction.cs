/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes
{
    internal class QuickFixSuggestedAction(
        IQuickFixApplication quickFixApplication,
        ITextBuffer textBuffer,
        IAnalysisIssueVisualization issueViz,
        IQuickFixesTelemetryManager quickFixesTelemetryManager,
        ILogger logger,
        IThreadHandling threadHandling)
        : BaseSuggestedAction
    {

        public override string DisplayText => Resources.ProductNameCommandPrefix + quickFixApplication.Message;

        public override void Invoke(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!quickFixApplication.CanBeApplied(textBuffer.CurrentSnapshot))
            {
                logger.LogVerbose("[Quick Fixes] Quick fix cannot be applied as the text has changed. Issue: " + issueViz.RuleId);
                return;
            }

            threadHandling.Run(async () =>
            {
                await threadHandling.SwitchToMainThreadAsync();
                await quickFixApplication.ApplyAsync(textBuffer.CurrentSnapshot, issueViz, cancellationToken);

                quickFixesTelemetryManager.QuickFixApplied(issueViz.RuleId);

                return 0;
            });
        }
    }
}
