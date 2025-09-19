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

using System.ComponentModel.Composition;
using System.Windows;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

public interface IDependencyRisksReportViewModel : IDisposable
{
    IGroupViewModel GetDependencyRisksGroup();

    Task ChangeDependencyRiskStatusAsync(IDependencyRisk dependencyRisk, DependencyRiskTransition? selectedTransition, string getNormalizedComment);

    void ShowDependencyRiskInBrowser(IDependencyRisk dependencyRisk);

    event EventHandler DependencyRisksChanged;
}

[Export(typeof(IDependencyRisksReportViewModel))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class DependencyRisksReportViewModel : IDependencyRisksReportViewModel
{
    private readonly IChangeDependencyRiskStatusHandler changeDependencyRiskStatusHandler;
    private readonly IDependencyRisksStore dependencyRisksStore;
    private readonly IMessageBox messageBox;
    private readonly IShowDependencyRiskInBrowserHandler showDependencyRiskInBrowserHandler;

    [ImportingConstructor]
    public DependencyRisksReportViewModel(
        IDependencyRisksStore dependencyRisksStore,
        IShowDependencyRiskInBrowserHandler showDependencyRiskInBrowserHandler,
        IChangeDependencyRiskStatusHandler changeDependencyRiskStatusHandler,
        IMessageBox messageBox)
    {
        this.dependencyRisksStore = dependencyRisksStore;
        this.showDependencyRiskInBrowserHandler = showDependencyRiskInBrowserHandler;
        this.changeDependencyRiskStatusHandler = changeDependencyRiskStatusHandler;
        this.messageBox = messageBox;
        dependencyRisksStore.DependencyRisksChanged += DependencyRisksStore_DependencyRiskChanged;
    }

    public void Dispose() => dependencyRisksStore.DependencyRisksChanged -= DependencyRisksStore_DependencyRiskChanged;

    public event EventHandler DependencyRisksChanged;

    public IGroupViewModel GetDependencyRisksGroup()
    {
        var groupDependencyRisk = new GroupDependencyRiskViewModel(dependencyRisksStore);
        groupDependencyRisk.InitializeRisks();
        return groupDependencyRisk.FilteredIssues.Any() ? groupDependencyRisk : null;
    }

    public async Task ChangeDependencyRiskStatusAsync(IDependencyRisk dependencyRisk, DependencyRiskTransition? selectedTransition, string getNormalizedComment)
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

    public void ShowDependencyRiskInBrowser(IDependencyRisk dependencyRisk) => showDependencyRiskInBrowserHandler.ShowInBrowser(dependencyRisk.Id);

    private void ShowFailureMessage(string errorMessage) => messageBox.Show(Resources.DependencyRiskStatusChangeFailedTitle, errorMessage, MessageBoxButton.OK, MessageBoxImage.Error);

    private void DependencyRisksStore_DependencyRiskChanged(object sender, EventArgs e) => DependencyRisksChanged?.Invoke(null, EventArgs.Empty);
}
