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
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Taints;

[TestClass]
public class TaintsReportViewModelTest
{
    private ITaintStore localTaintsStore;
    private TaintsReportViewModel testSubject;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        localTaintsStore = Substitute.For<ITaintStore>();
        threadHandling = Substitute.For<IThreadHandling>();
        testSubject = new TaintsReportViewModel(localTaintsStore, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<TaintsReportViewModel, ITaintsReportViewModel>(
            MefTestHelpers.CreateExport<ITaintStore>(),
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
    public void GetTaintsGroupViewModels_GroupsByFilePath()
    {
        var file1 = "file1.cs";
        var file2 = "file2.cs";
        MockTaintsInStore(CreateMockedTaint(file1), CreateMockedTaint(file1), CreateMockedTaint(file2));

        var groups = testSubject.GetTaintsGroupViewModels();

        groups.Should().HaveCount(2);
        groups.Select(g => g.Title).Should().Contain([file1, file2]);
        groups.First(g => g.Title == file1).FilteredIssues.Should().HaveCount(2);
        groups.First(g => g.Title == file2).FilteredIssues.Should().ContainSingle();
    }

    [TestMethod]
    public void GetTaintsGroupViewModels_TwoTaintsInSameFile_CreatesOneGroupVmWithTwoIssues()
    {
        var path = "myFile.cs";
        var taint1 = CreateMockedTaint(path);
        var taint2 = CreateMockedTaint(path);
        MockTaintsInStore(taint1, taint2);

        var groups = testSubject.GetTaintsGroupViewModels();

        groups.Should().ContainSingle();
        VerifyExpectedTaintGroupViewModel(groups[0] as GroupFileViewModel, taint1, taint2);
    }

    [TestMethod]
    public void GetTaintsGroupViewModels_TwoTaintsInDifferentFiles_CreatesTwoGroupsWithOneIssueEach()
    {
        var taint1 = CreateMockedTaint("myFile.cs");
        var taint2 = CreateMockedTaint("myFile.js");
        MockTaintsInStore(taint1, taint2);

        var groups = testSubject.GetTaintsGroupViewModels();

        groups.Should().HaveCount(2);
        VerifyExpectedTaintGroupViewModel(groups[0] as GroupFileViewModel, taint1);
        VerifyExpectedTaintGroupViewModel(groups[1] as GroupFileViewModel, taint2);
    }

    [TestMethod]
    public void TaintsChanged_RaisedOnStoreIssuesChanged()
    {
        var raised = false;
        testSubject.IssuesChanged += (_, _) => raised = true;

        localTaintsStore.IssuesChanged += Raise.Event<EventHandler<IssuesChangedEventArgs>>(null, null);

        raised.Should().BeTrue();
    }

    private static IAnalysisIssueVisualization CreateMockedTaint(string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var analysisIssueBase = Substitute.For<IAnalysisIssueBase>();
        analysisIssueBase.PrimaryLocation.FilePath.Returns(filePath);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);

        return analysisIssueVisualization;
    }

    private void MockTaintsInStore(params IAnalysisIssueVisualization[] taints) => localTaintsStore.GetAll().Returns(taints);

    private static void VerifyExpectedTaintGroupViewModel(GroupFileViewModel groupFileVm, params IAnalysisIssueVisualization[] expectedTaints)
    {
        groupFileVm.Should().NotBeNull();
        groupFileVm.FilePath.Should().Be(expectedTaints[0].Issue.PrimaryLocation.FilePath);
        groupFileVm.FilteredIssues.Should().HaveCount(expectedTaints.Length);
        foreach (var expectedTaint in expectedTaints)
        {
            groupFileVm.FilteredIssues.Should().ContainSingle(vm => ((TaintViewModel)vm).Issue == expectedTaint);
        }
    }
}
