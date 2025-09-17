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
    private readonly ITelemetryManager telemetryManager;
    private IIssueViewModel selectedItem;
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
        this.telemetryManager = telemetryManager;
        GroupDependencyRisk = new GroupDependencyRiskViewModel(dependencyRisksStore, [ResolutionFilterOpen, ResolutionFilterResolved], threadHandling);
        GroupDependencyRisk.InitializeRisks();
    }

    public ResolutionFilterViewModel ResolutionFilterOpen { get; } = new(false, true);
    public ResolutionFilterViewModel ResolutionFilterResolved { get; } = new(true, false);

    public IIssueViewModel SelectedItem
    {
        get => selectedItem;
        set
        {
            if (selectedItem != value)
            {
                selectedItem = value;
                UpdateTelemetry(selectedItem);
            }
        }
    }

    public void FlipAndUpdateResolutionFilter(ResolutionFilterViewModel viewModel)
    {
        viewModel.IsSelected = !viewModel.IsSelected;
        EnableOtherFilter(viewModel);
        GroupDependencyRisk.RefreshFiltering();
    }

    private void EnableOtherFilter(ResolutionFilterViewModel viewModel)
    {
        // this is done to not end up in a situation when both filters are disabled
        if (viewModel == ResolutionFilterOpen)
        {
            ResolutionFilterResolved.IsSelected = true;
        }
        if (viewModel == ResolutionFilterResolved)
        {
            ResolutionFilterOpen.IsSelected = true;
        }
    }

    public async Task ChangeStatusAsync(IDependencyRisk dependencyRisk, DependencyRiskTransition? selectedTransition, string getNormalizedComment)
    {
        if (selectedTransition is not { } transition)
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

    public void ShowInBrowser(IDependencyRisk dependencyRisk) => showDependencyRiskInBrowserHandler.ShowInBrowser(dependencyRisk.Id);

    protected override void Dispose(bool disposing)
    {
        GroupDependencyRisk.Dispose();
        base.Dispose(disposing);
    }

    private void UpdateTelemetry(IIssueViewModel issueViewModel)
    {
        if (issueViewModel is DependencyRiskViewModel)
        {
            telemetryManager.DependencyRiskInvestigatedLocally();
        }
    }
}
