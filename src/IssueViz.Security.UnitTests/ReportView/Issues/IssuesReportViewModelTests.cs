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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
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

    [TestInitialize]
    public void TestInitialize()
    {
        localIssuesStore = Substitute.For<ILocalIssuesStore>();
        showInBrowserService = Substitute.For<IShowInBrowserService>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();

        testSubject = new IssuesReportViewModel(localIssuesStore, showInBrowserService, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<IssuesReportViewModel, IIssuesReportViewModel>(
            MefTestHelpers.CreateExport<ILocalIssuesStore>(),
            MefTestHelpers.CreateExport<IShowInBrowserService>(),
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
        var eventHandler = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandler;

        localIssuesStore.IssuesChanged += Raise.Event<EventHandler<IssuesChangedEventArgs>>(null, null);

        Received.InOrder(() =>
        {
            threadHandling.RunOnUIThread(Arg.Any<Action>());
            eventHandler.Invoke(Arg.Any<object>(), Arg.Any<IssuesChangedEventArgs>());
        });
    }

    [TestMethod]
    public void ShowIssueInBrowser_CallsServiceWithCorrectArgumentAndSendTelemetry()
    {
        var issue = Substitute.For<IAnalysisIssue>();
        issue.IssueServerKey.Returns("key");

        testSubject.ShowIssueInBrowser(issue);

        showInBrowserService.Received(1).ShowIssue(issue.IssueServerKey);
    }

    private static IAnalysisIssueVisualization CreateMockedIssue(string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var analysisIssueBase = Substitute.For<IAnalysisIssue>();
        analysisIssueBase.PrimaryLocation.FilePath.Returns(filePath);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);
        return analysisIssueVisualization;
    }

    private void MockIssuesInStore(params IAnalysisIssueVisualization[] issues) => localIssuesStore.GetAll().Returns(issues);
}
