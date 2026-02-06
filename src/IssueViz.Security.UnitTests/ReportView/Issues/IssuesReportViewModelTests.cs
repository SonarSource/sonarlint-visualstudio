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

using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.ReviewStatus;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues.ReviewIssue;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Issues;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Issues;

[TestClass]
public class IssuesReportViewModelTests
{
    private ILocalIssuesStore localIssuesStore;
    private IssuesReportViewModel testSubject;
    private IShowInBrowserService showInBrowserService;
    private IThreadHandling threadHandling;
    private IReviewIssuesService reviewIssuesService;
    private IMessageBox messageBox;

    [TestInitialize]
    public void TestInitialize()
    {
        localIssuesStore = Substitute.For<ILocalIssuesStore>();
        showInBrowserService = Substitute.For<IShowInBrowserService>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        reviewIssuesService = Substitute.For<IReviewIssuesService>();
        messageBox = Substitute.For<IMessageBox>();

        testSubject = new IssuesReportViewModel(localIssuesStore, showInBrowserService, reviewIssuesService, messageBox, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<IssuesReportViewModel, IIssuesReportViewModel>(
            MefTestHelpers.CreateExport<ILocalIssuesStore>(),
            MefTestHelpers.CreateExport<IShowInBrowserService>(),
            MefTestHelpers.CreateExport<IReviewIssuesService>(),
            MefTestHelpers.CreateExport<IMessageBox>(),
            MefTestHelpers.CreateExport<IThreadHandling>()
        );

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<IssuesReportViewModel>();

    [TestMethod]
    public void Constructor_SubscribesToIssuesChanged() => localIssuesStore.Received().IssuesChanged += Arg.Any<EventHandler<IssuesChangedEventArgs>>();

    [TestMethod]
    public void Dispose_UnsubscribesFromIssuesChanged()
    {
        testSubject.Dispose();
        localIssuesStore.Received().IssuesChanged -= Arg.Any<EventHandler<IssuesChangedEventArgs>>();
    }

    [TestMethod]
    public void GetIssueViewModels_ReturnsIssuesFromStore()
    {
        var file1 = "file1.cs";
        var file2 = "file2.cs";
        IAnalysisIssueVisualization[] issues = [CreateMockedIssue(file1), CreateMockedIssue(file1), CreateMockedIssue(file2)];
        MockIssuesInStore(issues);

        var result = testSubject.GetIssueViewModels();

        result.Select(x => ((IssueViewModel)x).Issue).Should().BeEquivalentTo(issues);
    }

    [TestMethod]
    public void IssuesChanged_RaisedOnStoreIssuesChanged()
    {
        var eventHandler = Substitute.For<EventHandler<ViewModelAnalysisIssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandler;
        var addedIssue = CreateMockedIssue("addedFile.cs");
        var removedIssue = CreateMockedIssue("removedFile.cs");
        var removedId = removedIssue.IssueId;
        var eventArgs = new IssuesChangedEventArgs([removedIssue], [addedIssue]);

        localIssuesStore.IssuesChanged += Raise.Event<EventHandler<IssuesChangedEventArgs>>(null, eventArgs);

        Received.InOrder(() =>
        {
            threadHandling.RunOnUIThread(Arg.Any<Action>());
            eventHandler.Invoke(Arg.Any<object>(), Arg.Is<ViewModelAnalysisIssuesChangedEventArgs>(args =>
                args.AddedIssues.Count == 1
                && args.RemovedIssues.Count == 1
                && args.AddedIssues.OfType<IAnalysisIssueViewModel>().Single().Issue == addedIssue
                && args.RemovedIssues.Single() == removedId));
        });
    }

    [TestMethod]
    public void ShowIssueInBrowser_CallsServiceWithCorrectArgument()
    {
        var issue = Substitute.For<IAnalysisIssue>();
        issue.IssueServerKey.Returns("key");

        testSubject.ShowIssueInBrowser(issue);

        showInBrowserService.Received(1).ShowIssue(issue.IssueServerKey);
    }

    [TestMethod]
    public async Task GetAllowedStatusesAsync_NullViewModel_ShowsErrorAndReturnsNull()
    {
        var result = await testSubject.GetAllowedStatusesAsync(null);

        result.Should().BeNull();
        messageBox.Received(1).Show(
            Arg.Is<string>(s => s.Contains(Resources.ReviewIssueWindow_NoStatusSelectedFailureMessage)),
            Resources.ReviewIssueWindow_FailureTitle,
            Arg.Any<MessageBoxButton>(),
            MessageBoxImage.Error);
    }

    [TestMethod]
    public async Task GetAllowedStatusesAsync_ServiceReturnsPermitted_ReturnsAllowedStatuses()
    {
        var issueKey = "test-key";
        var issueViewModel = CreateIssueViewModel(issueKey);
        var allowedStatuses = new[] { SonarLint.VisualStudio.SLCore.Service.Issue.Models.ResolutionStatus.ACCEPT };
        reviewIssuesService.CheckReviewIssuePermittedAsync(issueKey)
            .Returns(new ReviewIssuePermittedArgs(allowedStatuses));

        var result = await testSubject.GetAllowedStatusesAsync(issueViewModel);

        result.Should().BeEquivalentTo(allowedStatuses);
        messageBox.DidNotReceive().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public async Task GetAllowedStatusesAsync_ServiceReturnsNotPermitted_ShowsErrorAndReturnsNull()
    {
        var issueKey = "test-key";
        var issueViewModel = CreateIssueViewModel(issueKey);
        var reason = "Not allowed";
        reviewIssuesService.CheckReviewIssuePermittedAsync(issueKey)
            .Returns(new ReviewIssueNotPermittedArgs(reason));

        var result = await testSubject.GetAllowedStatusesAsync(issueViewModel);

        result.Should().BeNull();
        messageBox.Received(1).Show(
            Arg.Is<string>(s => s.Contains(reason)),
            Resources.ReviewIssueWindow_FailureTitle,
            Arg.Any<MessageBoxButton>(),
            MessageBoxImage.Error);
    }

    [TestMethod]
    public async Task ChangeIssueStatusAsync_Success_ReturnsTrue()
    {
        var issueKey = "test-key";
        var issueViewModel = CreateIssueViewModel(issueKey);
        var status = SonarLint.VisualStudio.SLCore.Service.Issue.Models.ResolutionStatus.ACCEPT;
        var comment = "test comment";
        reviewIssuesService.ReviewIssueAsync(issueKey, status, comment).Returns(true);

        var result = await testSubject.ChangeIssueStatusAsync(issueViewModel, status, comment);

        result.Should().BeTrue();
        messageBox.DidNotReceive().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public async Task ChangeIssueStatusAsync_Failure_ShowsErrorAndReturnsFalse()
    {
        var issueKey = "test-key";
        var issueViewModel = CreateIssueViewModel(issueKey);
        var status = SonarLint.VisualStudio.SLCore.Service.Issue.Models.ResolutionStatus.ACCEPT;
        var comment = "test comment";
        reviewIssuesService.ReviewIssueAsync(issueKey, status, comment).Returns(false);

        var result = await testSubject.ChangeIssueStatusAsync(issueViewModel, status, comment);

        result.Should().BeFalse();
        messageBox.Received(1).Show(
            Resources.ReviewIssueWindow_ReviewFailureMessage,
            Resources.ReviewIssueWindow_FailureTitle,
            Arg.Any<MessageBoxButton>(),
            MessageBoxImage.Error);
    }

    [TestMethod]
    public async Task ReopenIssueAsync_Success_ReturnsTrue()
    {
        var issueKey = "test-key";
        var issueViewModel = CreateIssueViewModel(issueKey);
        reviewIssuesService.ReopenIssueAsync(issueKey).Returns(true);

        var result = await testSubject.ReopenIssueAsync(issueViewModel);

        result.Should().BeTrue();
        messageBox.DidNotReceive().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public async Task ReopenIssueAsync_Failure_ShowsErrorAndReturnsFalse()
    {
        var issueKey = "test-key";
        var issueViewModel = CreateIssueViewModel(issueKey);
        reviewIssuesService.ReopenIssueAsync(issueKey).Returns(false);

        var result = await testSubject.ReopenIssueAsync(issueViewModel);

        result.Should().BeFalse();
        messageBox.Received(1).Show(
            Resources.ReviewIssueWindow_ReviewFailureMessage,
            Resources.ReviewIssueWindow_FailureTitle,
            Arg.Any<MessageBoxButton>(),
            MessageBoxImage.Error);
    }

    private static IssueViewModel CreateIssueViewModel(string issueKey)
    {
        var analysisIssue = Substitute.For<IAnalysisIssue>();
        analysisIssue.IssueServerKey.Returns(issueKey);
        var issueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        issueVisualization.Issue.Returns(analysisIssue);
        return new IssueViewModel(issueVisualization);
    }

    private static IAnalysisIssueVisualization CreateMockedIssue(string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var analysisIssueBase = Substitute.For<IAnalysisIssue>();
        analysisIssueBase.PrimaryLocation.FilePath.Returns(filePath);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);
        analysisIssueVisualization.IssueId.Returns(Guid.NewGuid());
        return analysisIssueVisualization;
    }

    private void MockIssuesInStore(params IAnalysisIssueVisualization[] issues) => localIssuesStore.GetAll().Returns(issues);
}
