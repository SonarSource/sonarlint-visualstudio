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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;

internal class QuickFixSuggestedAction(
    IQuickFixApplication quickFixApplication,
    ITextBuffer textBuffer,
    IAnalysisIssueVisualization issueViz,
    IQuickFixApplicationLogic quickFixApplicationLogic,
    IThreadHandling threadHandling,
    ILightBulbBroker lightBulbBroker,
    ITextView textView)
    : BaseSuggestedAction
{
    public override string DisplayText => Resources.ProductNameCommandPrefix + quickFixApplication.Message;

    public override void Invoke(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        threadHandling.Run(async () =>
        {
            await threadHandling.SwitchToMainThreadAsync();
            lightBulbBroker.DismissSession(textView);
            await quickFixApplicationLogic.ApplyAsync(quickFixApplication, textBuffer.CurrentSnapshot, issueViz, cancellationToken);
            return 0;
        });
    }
}
