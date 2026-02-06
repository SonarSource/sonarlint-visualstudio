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
using SonarLint.VisualStudio.ConnectedMode.Transition;
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
    private IMuteIssuesService muteIssuesService;
    private IMessageBox messageBox;

    [TestInitialize]
    public void TestInitialize()
    {
        localIssuesStore = Substitute.For<ILocalIssuesStore>();
        showInBrowserService = Substitute.For<IShowInBrowserService>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        reviewIssuesService = Substitute.For<IReviewIssuesService>();
        muteIssuesService = Substitute.For<IMuteIssuesService>();
        messageBox = Substitute.For<IMessageBox>();

        testSubject = new IssuesReportViewModel(localIssuesStore, showInBrowserService, reviewIssuesService, muteIssuesService, messageBox, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<IssuesReportViewModel, IIssuesReportViewModel>(
            MefTestHelpers.CreateExport<ILocalIssuesStore>(),
            MefTestHelpers.CreateExport<IShowInBrowserService>(),
            MefTestHelpers.CreateExport<IReviewIssuesService>(),
            MefTestHelpers.CreateExport<IMuteIssuesService>(),
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
    public void ResolveIssueWithDialog_WithValidIssue_CallsMuteIssuesService()
    {
        var issueVm = CreateIssueViewModel("server-key-123");

        testSubject.ResolveIssueWithDialog(issueVm);

        muteIssuesService.Received(1).ResolveIssueWithDialog("server-key-123", false);
    }

    [TestMethod]
    public void ReopenIssue_WithValidIssue_CallsMuteIssuesService()
    {
        var issueVm = CreateIssueViewModel("server-key-456");

        testSubject.ReopenIssue(issueVm);

        muteIssuesService.Received(1).ReopenIssue("server-key-456", false);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenIssueServerKeyIsNull_DoesNotCallService()
    {
        var issueVm = CreateIssueViewModel(null);

        testSubject.ResolveIssueWithDialog(issueVm);

        muteIssuesService.DidNotReceive().ResolveIssueWithDialog(Arg.Any<string>(), Arg.Any<bool>());
    }

    [TestMethod]
    public void ReopenIssue_WhenIssueServerKeyIsNull_DoesNotCallService()
    {
        var issueVm = CreateIssueViewModel(null);

        testSubject.ReopenIssue(issueVm);

        muteIssuesService.DidNotReceive().ReopenIssue(Arg.Any<string>(), Arg.Any<bool>());
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenIssueViewModelIsNull_DoesNotCallService()
    {
        testSubject.ResolveIssueWithDialog(null);

        muteIssuesService.DidNotReceive().ResolveIssueWithDialog(Arg.Any<string>(), Arg.Any<bool>());
    }

    [TestMethod]
    public void ReopenIssue_WhenIssueViewModelIsNull_DoesNotCallService()
    {
        testSubject.ReopenIssue(null);

        muteIssuesService.DidNotReceive().ReopenIssue(Arg.Any<string>(), Arg.Any<bool>());
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
