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

using System.Windows;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal class ReportViewModel : ServerViewModel
{
    private readonly IShowDependencyRiskInBrowserHandler showDependencyRiskInBrowserHandler;
    private readonly IChangeDependencyRiskStatusHandler changeDependencyRiskStatusHandler;
    private readonly IMessageBox messageBox;
    public GroupDependencyRiskViewModel GroupDependencyRisk { get; }

    public ReportViewModel(
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IDependencyRisksStore dependencyRisksStore,
        IShowDependencyRiskInBrowserHandler showDependencyRiskInBrowserHandler,
        IChangeDependencyRiskStatusHandler changeDependencyRiskStatusHandler,
        IMessageBox messageBox,
        ITelemetryManager telemetryManager,
        IThreadHandling threadHandling) : base(activeSolutionBoundTracker)
    {
        this.showDependencyRiskInBrowserHandler = showDependencyRiskInBrowserHandler;
        this.changeDependencyRiskStatusHandler = changeDependencyRiskStatusHandler;
        this.messageBox = messageBox;
        GroupDependencyRisk = new GroupDependencyRiskViewModel(dependencyRisksStore, telemetryManager, threadHandling);
        GroupDependencyRisk.InitializeRisks();
    }

    protected override void Dispose(bool disposing)
    {
        GroupDependencyRisk.Dispose();
        base.Dispose(disposing);
    }

    public async Task ChangeStatusAsync(IDependencyRisk dependencyRisk, DependencyRiskTransition? selectedTransition, string getNormalizedComment)
    {
        if (selectedTransition is not {} transition)
        {
            ShowFailureMessage(Resources.DependencyRiskNullTransitionError);
            return;
        }

        var result = await changeDependencyRiskStatusHandler.ChangeStatusAsync(dependencyRisk.Id, transition, getNormalizedComment);

        if (!result)
        {
            ShowFailureMessage(Resources.DependencyRiskStatusChangeError);
        }
    }

    private void ShowFailureMessage(string errorMessage) => messageBox.Show(Resources.DependencyRiskStatusChangeFailedTitle, errorMessage, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInBrowser(IDependencyRisk dependencyRisk) =>
        showDependencyRiskInBrowserHandler.ShowInBrowser(dependencyRisk.Id);
}
