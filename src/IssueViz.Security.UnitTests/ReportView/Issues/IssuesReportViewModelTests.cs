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

using SonarLint.VisualStudio.ConnectedMode.ReviewStatus;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Issues;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Issues;

[TestClass]
public class IssuesReportViewModelTests
{
    private ILocalIssuesStore localIssuesStore = null!;
    private IssuesReportViewModel testSubject = null!;
    private IShowInBrowserService showInBrowserService = null!;
    private IThreadHandling threadHandling = null!;
    private IMuteIssuesService muteIssuesService = null!;
    private IQuickFixService quickFixService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        localIssuesStore = Substitute.For<ILocalIssuesStore>();
        showInBrowserService = Substitute.For<IShowInBrowserService>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        muteIssuesService = Substitute.For<IMuteIssuesService>();
        quickFixService = Substitute.For<IQuickFixService>();

        testSubject = new IssuesReportViewModel(localIssuesStore, showInBrowserService, muteIssuesService, quickFixService, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<IssuesReportViewModel, IIssuesReportViewModel>(
            MefTestHelpers.CreateExport<ILocalIssuesStore>(),
            MefTestHelpers.CreateExport<IShowInBrowserService>(),
            MefTestHelpers.CreateExport<IMuteIssuesService>(),
            MefTestHelpers.CreateExport<IQuickFixService>(),
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

    [TestMethod]
    public void GetApplicableQuickFixes_NoQuickFixes_ReturnsEmpty()
    {
        var issueVm = CreateIssueViewModelWithQuickFixes(null);

        var result = testSubject.GetApplicableQuickFixes(issueVm);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void GetApplicableQuickFixes_EmptyQuickFixes_ReturnsEmpty()
    {
        var issueVm = CreateIssueViewModelWithQuickFixes([]);

        var result = testSubject.GetApplicableQuickFixes(issueVm);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void GetApplicableQuickFixes_SomeApplicable_ReturnsFilteredList()
    {
        var fix1 = Substitute.For<IQuickFixApplication>();
        fix1.Message.Returns("Fix 1");
        var fix2 = Substitute.For<IQuickFixApplication>();
        fix2.Message.Returns("Fix 2");
        var issueVm = CreateIssueViewModelWithQuickFixes([fix1, fix2]);
        quickFixService.CanBeApplied(fix1, issueVm.FilePath).Returns(true);
        quickFixService.CanBeApplied(fix2, issueVm.FilePath).Returns(false);

        var result = testSubject.GetApplicableQuickFixes(issueVm);

        result.Should().HaveCount(1);
        result[0].QuickFix.Should().BeSameAs(fix1);
        result[0].Message.Should().Be("Fix 1");
        result[0].FilePath.Should().Be(issueVm.FilePath);
        result[0].IssueViz.Should().BeSameAs(issueVm.Issue);
    }

    [TestMethod]
    public void GetApplicableQuickFixes_NoneApplicable_ReturnsEmpty()
    {
        var fix1 = Substitute.For<IQuickFixApplication>();
        var issueVm = CreateIssueViewModelWithQuickFixes([fix1]);
        quickFixService.CanBeApplied(fix1, issueVm.FilePath).Returns(false);

        var result = testSubject.GetApplicableQuickFixes(issueVm);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ApplyQuickFixAsync_DelegatesToQuickFixService()
    {
        var fix = Substitute.For<IQuickFixApplication>();
        var issueViz = Substitute.For<IAnalysisIssueVisualization>();
        var quickFixVm = new QuickFixViewModel(fix, issueViz, "test.cs");

        await testSubject.ApplyQuickFixAsync(quickFixVm);

        await quickFixService.Received(1).ApplyAsync(fix, "test.cs", issueViz, Arg.Any<CancellationToken>());
    }

    private static IssueViewModel CreateIssueViewModelWithQuickFixes(IReadOnlyList<IQuickFixApplication> quickFixes)
    {
        var analysisIssue = Substitute.For<IAnalysisIssue>();
        analysisIssue.PrimaryLocation.FilePath.Returns("test-file.cs");
        var issueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        issueVisualization.Issue.Returns(analysisIssue);
        issueVisualization.QuickFixes.Returns(quickFixes);
        return new IssueViewModel(issueVisualization);
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
