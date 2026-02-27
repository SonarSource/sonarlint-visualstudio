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
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues;

namespace SonarLint.VisualStudio.Integration.Vsix.Events.Build;

[Export(typeof(IBuildEventUiManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class BuildEventUiManager(
    ISonarLintSettings settings,
    IErrorNotificationDialogService errorNotificationDialogService,
    IToolWindowService toolWindowService,
    ILocalIssuesStore localIssuesStore,
    IThreadHandling threadHandling) : IBuildEventUiManager
{
    public void ShowErrorNotificationDialog()
    {
        if (!settings.IsShowBuildErrorNotificationEnabled)
        {
            return;
        }

        var issues = localIssuesStore.GetAll();
        var errorCount = issues.Count(i => i.VsSeverity == __VSERRORCATEGORY.EC_ERROR);
        if (errorCount == 0)
        {
            return;
        }

        (bool okClicked, bool doNotShowAgain) dialogResult = default;
        threadHandling.RunOnUIThread(() =>
        {
            dialogResult = errorNotificationDialogService.ShowDialog(errorCount);
        });

        if (dialogResult.doNotShowAgain)
        {
            settings.IsShowBuildErrorNotificationEnabled = false;
        }

        if (dialogResult.okClicked)
        {
            toolWindowService.Show(IssueListIds.ErrorListId);
        }
    }
}
