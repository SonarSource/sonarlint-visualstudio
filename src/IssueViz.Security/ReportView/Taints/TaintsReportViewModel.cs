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
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.ReviewStatus;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues.ReviewIssue;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;

internal interface ITaintsReportViewModel : IDisposable
{
    void ShowTaintInBrowser(ITaintIssue taintIssue);

    void ShowIssueVisualization();

    Task<IEnumerable<ResolutionStatus>> GetAllowedStatusesAsync(TaintViewModel selectedTaintViewModel);

    Task<bool> ChangeTaintStatusAsync(TaintViewModel selectedTaintViewModel, ResolutionStatus newStatus, string comment);

    Task<bool> ReopenTaintAsync(TaintViewModel selectedTaintViewModel);

    IEnumerable<IIssueViewModel> GetIssueViewModels();

    event EventHandler<ViewModelAnalysisIssuesChangedEventArgs> IssuesChanged;
}

internal interface IFileAwareTaintsReportViewModel : ITaintsReportViewModel;

internal abstract class TaintsReportViewModelBase(
    ITaintStoreReader taintsStore,
    IShowInBrowserService showInBrowserService,
    IReviewIssuesService reviewIssuesService,
    IMessageBox messageBox,
    ITelemetryManager telemetryManager,
    IThreadHandling threadHandling)
    : IssuesReportViewModelBase(taintsStore, threadHandling)
{
    public void ShowTaintInBrowser(ITaintIssue taintIssue)
    {
        telemetryManager.TaintIssueInvestigatedRemotely();
        showInBrowserService.ShowIssue(taintIssue.IssueServerKey);
    }

    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    public void ShowIssueVisualization() => ToolWindowNavigator.Instance.ShowIssueVisualizationToolWindow();

    public async Task<IEnumerable<ResolutionStatus>> GetAllowedStatusesAsync(TaintViewModel selectedTaintViewModel)
    {
        var response = selectedTaintViewModel == null
            ? new ReviewIssueNotPermittedArgs(Resources.ReviewIssueWindow_NoStatusSelectedFailureMessage)
            : await reviewIssuesService.CheckReviewIssuePermittedAsync(selectedTaintViewModel.TaintIssue.IssueServerKey);
        switch (response)
        {
            case ReviewIssuePermittedArgs reviewIssuePermittedArgs:
                return reviewIssuePermittedArgs.AllowedStatuses;
            case ReviewIssueNotPermittedArgs reviewIssueNotPermittedArgs:
                messageBox.Show(string.Format(Resources.ReviewIssueWindow_CheckReviewPermittedFailureMessage, reviewIssueNotPermittedArgs.Reason), Resources.ReviewIssueWindow_FailureTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                break;
        }
        return null;
    }

    public async Task<bool> ChangeTaintStatusAsync(TaintViewModel selectedTaintViewModel, ResolutionStatus newStatus, string comment)
    {
        var wasChanged = await reviewIssuesService.ReviewIssueAsync(selectedTaintViewModel.TaintIssue.IssueServerKey, newStatus, comment, isTaint: true);
        if (!wasChanged)
        {
            messageBox.Show(Resources.ReviewIssueWindow_ReviewFailureMessage, Resources.ReviewIssueWindow_FailureTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        return wasChanged;
    }

    public async Task<bool> ReopenTaintAsync(TaintViewModel selectedTaintViewModel)
    {
        var wasReopened = await reviewIssuesService.ReopenIssueAsync(selectedTaintViewModel.TaintIssue.IssueServerKey, isTaint: true);
        if (!wasReopened)
        {
            messageBox.Show(Resources.ReviewIssueWindow_ReviewFailureMessage, Resources.ReviewIssueWindow_FailureTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        return wasReopened;
    }

    public IEnumerable<IIssueViewModel> GetIssueViewModels() => taintsStore.GetAll().Select(CreateViewModel);
}

[Export(typeof(ITaintsReportViewModel))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class TaintsReportViewModel(
    ITaintStore taintsStore,
    IShowInBrowserService showInBrowserService,
    IReviewIssuesService reviewIssuesService,
    IMessageBox messageBox,
    ITelemetryManager telemetryManager,
    IThreadHandling threadHandling)
    : TaintsReportViewModelBase(taintsStore, showInBrowserService, reviewIssuesService, messageBox, telemetryManager, threadHandling), ITaintsReportViewModel
{
    protected override IIssueViewModel CreateViewModel(IAnalysisIssueVisualization issue) => new TaintViewModel(issue, true);
}

[Export(typeof(IFileAwareTaintsReportViewModel))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class FileAwareTaintsReportViewModel(
    IFileAwareTaintStore taintsStore,
    IShowInBrowserService showInBrowserService,
    IReviewIssuesService reviewIssuesService,
    IMessageBox messageBox,
    ITelemetryManager telemetryManager,
    IThreadHandling threadHandling)
    : TaintsReportViewModelBase(taintsStore, showInBrowserService, reviewIssuesService, messageBox, telemetryManager, threadHandling), IFileAwareTaintsReportViewModel
{
    protected override IIssueViewModel CreateViewModel(IAnalysisIssueVisualization issue) => new TaintViewModel(issue);
}
