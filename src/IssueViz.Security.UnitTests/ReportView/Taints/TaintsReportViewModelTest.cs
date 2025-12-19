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

using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Taints;

[TestClass]
public class TaintsReportViewModelTest
{
    private ITaintStore localTaintsStore;
    private TaintsReportViewModel testSubject;
    private IShowInBrowserService showInBrowserService;
    private IMuteIssuesService muteIssuesService;
    private ITelemetryManager telemetryManager;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        localTaintsStore = Substitute.For<ITaintStore>();
        showInBrowserService = Substitute.For<IShowInBrowserService>();
        muteIssuesService = Substitute.For<IMuteIssuesService>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();

        testSubject = new TaintsReportViewModel(localTaintsStore, showInBrowserService, muteIssuesService, telemetryManager, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<TaintsReportViewModel, ITaintsReportViewModel>(
            MefTestHelpers.CreateExport<ITaintStore>(),
            MefTestHelpers.CreateExport<IShowInBrowserService>(),
            MefTestHelpers.CreateExport<IMuteIssuesService>(),
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
    }

    [TestMethod]
    public void HotspotsChanged_RaisedOnStoreIssuesChanged()
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
                && args.AddedIssues.OfType<IAnalysisIssueViewModel>().Single().Issue == addedIssue
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
    public void ChangeStatus_CallsServiceWithCorrectArgumentAndTaintFlag()
    {
        var issue = Substitute.For<IAnalysisIssueVisualization>();

        testSubject.ChangeStatus(issue);

        muteIssuesService.Received(1).ResolveIssueWithDialog(issue, isTaintIssue: true);
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
