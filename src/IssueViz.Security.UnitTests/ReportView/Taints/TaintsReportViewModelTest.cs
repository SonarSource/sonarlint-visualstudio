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
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues.ReviewIssue;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Taints;

[TestClass]
public class TaintsReportViewModelTest
{
    private ITaintStore localTaintsStore;
    private IShowInBrowserService showInBrowserService;
    private IReviewIssuesService reviewIssuesService;
    private IMuteIssuesService muteIssuesService;
    private IMessageBox messageBox;
    private ITelemetryManager telemetryManager;
    private IThreadHandling threadHandling;
    private TaintsReportViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        localTaintsStore = Substitute.For<ITaintStore>();
        showInBrowserService = Substitute.For<IShowInBrowserService>();
        reviewIssuesService = Substitute.For<IReviewIssuesService>();
        muteIssuesService = Substitute.For<IMuteIssuesService>();
        messageBox = Substitute.For<IMessageBox>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();

        testSubject = new TaintsReportViewModel(localTaintsStore, showInBrowserService, reviewIssuesService, muteIssuesService, messageBox, telemetryManager, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<TaintsReportViewModel, ITaintsReportViewModel>(
            MefTestHelpers.CreateExport<ITaintStore>(),
            MefTestHelpers.CreateExport<IShowInBrowserService>(),
            MefTestHelpers.CreateExport<IReviewIssuesService>(),
            MefTestHelpers.CreateExport<IMuteIssuesService>(),
            MefTestHelpers.CreateExport<IMessageBox>(),
            MefTestHelpers.CreateExport<ITelemetryManager>(),
            MefTestHelpers.CreateExport<IThreadHandling>()
        );

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<TaintsReportViewModel>();

    [TestMethod]
    public void Constructor_SubscribesToIssuesChanged() => localTaintsStore.Received().IssuesChanged += Arg.Any<EventHandler<IssuesChangedEventArgs>>();

    [TestMethod]
    public void Dispose_UnsubscribesFromIssuesChanged()
    {
        testSubject.Dispose();

        localTaintsStore.Received().IssuesChanged -= Arg.Any<EventHandler<IssuesChangedEventArgs>>();
    }

    [TestMethod]
    public void GetIssueViewModels_ReturnsIssuesFromStore()
    {
        var file1 = "file1.cs";
        var file2 = "file2.cs";
        IAnalysisIssueVisualization[] taints = [CreateMockedTaint(file1), CreateMockedTaint(file1), CreateMockedTaint(file2)];
        MockTaintsInStore(taints);

        var issues = testSubject.GetIssueViewModels();

        issues.Select(x => ((TaintViewModel)x).Issue).Should().BeEquivalentTo(taints);
        issues.Select(x => ((TaintViewModel)x).IsSolutionLevelTaintDisplay).Should().AllBeEquivalentTo(true);
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
                && args.AddedIssues.OfType<TaintViewModel>().Single().IsSolutionLevelTaintDisplay
                && args.RemovedIssues.Single() == removedId));
        });
    }

    [TestMethod]
    public void ShowTaintInBrowserAsync_CallsServiceWithCorrectArgumentAndSendTelemetry()
    {
        var taintIssue = Substitute.For<ITaintIssue>();
        taintIssue.IssueServerKey.Returns("key");

        testSubject.ShowTaintInBrowser(taintIssue);

        showInBrowserService.Received(1).ShowIssue(taintIssue.IssueServerKey);
        telemetryManager.Received(1).TaintIssueInvestigatedRemotely();
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WithValidTaint_CallsMuteIssuesService()
    {
        var taintVm = CreateTaintViewModel("taint-key-123");

        testSubject.ResolveIssueWithDialog(taintVm);

        muteIssuesService.Received(1).ResolveIssueWithDialog("taint-key-123", true);
    }

    [TestMethod]
    public void ReopenIssue_WithValidTaint_CallsMuteIssuesService()
    {
        var taintVm = CreateTaintViewModel("taint-key-456");

        testSubject.ReopenIssue(taintVm);

        muteIssuesService.Received(1).ReopenIssue("taint-key-456", true);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenIssueServerKeyIsNull_DoesNotCallService()
    {
        var taintVm = CreateTaintViewModel(null);

        testSubject.ResolveIssueWithDialog(taintVm);

        muteIssuesService.DidNotReceive().ResolveIssueWithDialog(Arg.Any<string>(), Arg.Any<bool>());
    }

    [TestMethod]
    public void ReopenIssue_WhenIssueServerKeyIsNull_DoesNotCallService()
    {
        var taintVm = CreateTaintViewModel(null);

        testSubject.ReopenIssue(taintVm);

        muteIssuesService.DidNotReceive().ReopenIssue(Arg.Any<string>(), Arg.Any<bool>());
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenTaintViewModelIsNull_DoesNotCallService()
    {
        testSubject.ResolveIssueWithDialog(null);

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
        return new TaintViewModel(issueVisualization, true);
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
