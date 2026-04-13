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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Issues;

internal interface IIssuesReportViewModel : IDisposable
{
    IEnumerable<IIssueViewModel> GetIssueViewModels();
    event EventHandler<ViewModelAnalysisIssuesChangedEventArgs> IssuesChanged;

    void ShowIssueInBrowser(IAnalysisIssue issue);

    void ResolveIssueWithDialog(IssueViewModel issueViewModel);
    void ReopenIssue(IssueViewModel issueViewModel);

    void ShowIssueVisualization();

    IReadOnlyList<QuickFixViewModel> GetApplicableQuickFixes(IssueViewModel issueViewModel);
    Task ApplyQuickFixAsync(QuickFixViewModel quickFix);
}

[Export(typeof(IIssuesReportViewModel))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method:ImportingConstructor]
internal sealed class IssuesReportViewModel(
    ILocalIssuesStore localIssuesStore,
    IShowInBrowserService showInBrowserService,
    IMuteIssuesService muteIssuesService,
    IQuickFixService quickFixService,
    IThreadHandling threadHandling) : IssuesReportViewModelBase(localIssuesStore, threadHandling), IIssuesReportViewModel
{
    public IEnumerable<IIssueViewModel> GetIssueViewModels() => localIssuesStore.GetAll().Select(x => new IssueViewModel(x));

    public void ShowIssueInBrowser(IAnalysisIssue issue) => showInBrowserService.ShowIssue(issue.IssueServerKey);

    public void ResolveIssueWithDialog(IssueViewModel issueViewModel)
    {
        if (issueViewModel?.Issue?.Issue?.IssueServerKey is not { } issueServerKey)
        {
            return;
        }

        muteIssuesService.ResolveIssueWithDialog(issueServerKey, isTaintIssue: false);
    }

    public void ReopenIssue(IssueViewModel issueViewModel)
    {
        if (issueViewModel?.Issue?.Issue?.IssueServerKey is not { } issueServerKey)
        {
            return;
        }

        muteIssuesService.ReopenIssue(issueServerKey, isTaintIssue: false);
    }

    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    public void ShowIssueVisualization() => ToolWindowNavigator.Instance.ShowIssueVisualizationToolWindow();

    public IReadOnlyList<QuickFixViewModel> GetApplicableQuickFixes(IssueViewModel issueViewModel)
    {
        var quickFixes = issueViewModel.Issue.QuickFixes;
        if (quickFixes == null || !quickFixes.Any())
        {
            return [];
        }

        return quickFixes
            .Where(fix => quickFixService.CanBeApplied(fix, issueViewModel.FilePath))
            .Select(fix => new QuickFixViewModel(fix, issueViewModel.Issue, issueViewModel.FilePath))
            .ToList();
    }

    public async Task ApplyQuickFixAsync(QuickFixViewModel quickFix)
    {
        await quickFixService.ApplyAsync(quickFix.QuickFix, quickFix.FilePath, quickFix.IssueViz);
    }

    protected override IIssueViewModel CreateViewModel(IAnalysisIssueVisualization issue) => new IssueViewModel(issue);
}
