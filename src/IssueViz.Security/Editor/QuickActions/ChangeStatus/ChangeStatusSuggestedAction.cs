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

using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Editor.QuickActions.ChangeStatus;

internal class ChangeStatusSuggestedAction(
    IAnalysisIssueVisualization issueVisualization,
    IMuteIssuesService muteIssuesService)
    : BaseSuggestedAction
{
    public override string DisplayText => Resources.ChangeStatusActionText;

    public override void Invoke(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        muteIssuesService.ResolveIssueWithDialog(issueVisualization.IssueServerKey, isTaintIssue: false);
    }
}
