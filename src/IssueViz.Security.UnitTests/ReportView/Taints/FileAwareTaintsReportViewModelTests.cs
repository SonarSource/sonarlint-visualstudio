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

using SonarLint.VisualStudio.ConnectedMode.ReviewStatus;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues.ReviewIssue;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Taints;

[TestClass]
public class FileAwareTaintsReportViewModelTests
{
    private IFileAwareTaintStore localTaintsStore;
    private IShowInBrowserService showInBrowserService;
    private IReviewIssuesService reviewIssuesService;
    private IMuteIssuesService muteIssuesService;
    private IMessageBox messageBox;
    private ITelemetryManager telemetryManager;
    private IThreadHandling threadHandling;
    private FileAwareTaintsReportViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        localTaintsStore = Substitute.For<IFileAwareTaintStore>();
        showInBrowserService = Substitute.For<IShowInBrowserService>();
        reviewIssuesService = Substitute.For<IReviewIssuesService>();
        muteIssuesService = Substitute.For<IMuteIssuesService>();
        messageBox = Substitute.For<IMessageBox>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();

        testSubject = new FileAwareTaintsReportViewModel(localTaintsStore, showInBrowserService, muteIssuesService, telemetryManager, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<FileAwareTaintsReportViewModel, IFileAwareTaintsReportViewModel>(
            MefTestHelpers.CreateExport<IFileAwareTaintStore>(),
            MefTestHelpers.CreateExport<IShowInBrowserService>(),
            MefTestHelpers.CreateExport<IMuteIssuesService>(),
            MefTestHelpers.CreateExport<ITelemetryManager>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<FileAwareTaintsReportViewModel>();

    [TestMethod]
    public void ShouldInheritFromBaseClass() =>
        testSubject.Should().BeAssignableTo<TaintsReportViewModelBase>(); // rest of the tests are in TaintsReportViewModelTest

    [TestMethod]
    public void GetIssueViewModels_ReturnsIssuesFromStore()
    {
        var file1 = "file1.cs";
        var file2 = "file2.cs";
        IAnalysisIssueVisualization[] taints = [CreateMockedTaint(file1), CreateMockedTaint(file1), CreateMockedTaint(file2)];
        MockTaintsInStore(taints);

        var issues = testSubject.GetIssueViewModels();

        issues.Select(x => ((TaintViewModel)x).Issue).Should().BeEquivalentTo(taints);
        issues.Select(x => ((TaintViewModel)x).IsSolutionLevelTaintDisplay).Should().AllBeEquivalentTo(false);
    }

    [TestMethod]
    public void IssuesChanged_RaisedOnStoreIssuesChanged()
    {
        var eventHandler = Substitute.For<EventHandler<ViewModelAnalysisIssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandler;
        var addedIssue = CreateMockedTaint("addedFile.cs");
        var removedIssue = CreateMockedTaint("removedFile.cs");
        var removedId = removedIssue.IssueId;
        var eventArgs = new IssuesChangedEventArgs([removedIssue], [addedIssue]);

        localTaintsStore.IssuesChanged += Raise.Event<EventHandler<IssuesChangedEventArgs>>(null, eventArgs);

        Received.InOrder(() =>
        {
            threadHandling.RunOnUIThread(Arg.Any<Action>());
            eventHandler.Invoke(Arg.Any<object>(), Arg.Is<ViewModelAnalysisIssuesChangedEventArgs>(args =>
                args.AddedIssues.Count == 1
                && args.RemovedIssues.Count == 1
                && args.AddedIssues.OfType<TaintViewModel>().Single().Issue == addedIssue
                && !args.AddedIssues.OfType<TaintViewModel>().Single().IsSolutionLevelTaintDisplay
                && args.RemovedIssues.Single() == removedId));
        });
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WithValidTaint_CallsMuteIssuesService()
    {
        var taintVm = CreateTaintViewModel("taint-key-789");

        testSubject.ResolveIssueWithDialog(taintVm);

        muteIssuesService.Received(1).ResolveIssueWithDialog("taint-key-789", true);
    }

    [TestMethod]
    public void ReopenIssue_WithValidTaint_CallsMuteIssuesService()
    {
        var taintVm = CreateTaintViewModel("taint-key-abc");

        testSubject.ReopenIssue(taintVm);

        muteIssuesService.Received(1).ReopenIssue("taint-key-abc", true);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenIssueServerKeyIsNull_DoesNotCallService()
    {
        var taintVm = CreateTaintViewModel(null);

        testSubject.ResolveIssueWithDialog(taintVm);

        muteIssuesService.DidNotReceive().ResolveIssueWithDialog(Arg.Any<string>(), Arg.Any<bool>());
    }

    [TestMethod]
    public void ReopenIssue_WhenTaintViewModelIsNull_DoesNotCallService()
    {
        testSubject.ReopenIssue(null);

        muteIssuesService.DidNotReceive().ReopenIssue(Arg.Any<string>(), Arg.Any<bool>());
    }

    private static TaintViewModel CreateTaintViewModel(string issueKey)
    {
        var taintIssue = Substitute.For<ITaintIssue>();
        taintIssue.IssueServerKey.Returns(issueKey);
        var issueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        issueVisualization.Issue.Returns(taintIssue);
        return new TaintViewModel(issueVisualization);
    }

    private static IAnalysisIssueVisualization CreateMockedTaint(string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var analysisIssueBase = Substitute.For<ITaintIssue>();
        analysisIssueBase.PrimaryLocation.FilePath.Returns(filePath);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);

        return analysisIssueVisualization;
    }

    private void MockTaintsInStore(params IAnalysisIssueVisualization[] taints) => localTaintsStore.GetAll().Returns(taints);
}
