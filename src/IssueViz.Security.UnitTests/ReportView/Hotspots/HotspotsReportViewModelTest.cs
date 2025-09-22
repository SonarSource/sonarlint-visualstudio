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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Hotspots;

[TestClass]
public class HotspotsReportViewModelTest
{
    private ILocalHotspotsStore localHotspotsStore;
    private HotspotsReportViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        localHotspotsStore = Substitute.For<ILocalHotspotsStore>();
        testSubject = new HotspotsReportViewModel(localHotspotsStore);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<HotspotsReportViewModel, IHotspotsReportViewModel>(
            MefTestHelpers.CreateExport<ILocalHotspotsStore>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<HotspotsReportViewModel>();

    [TestMethod]
    public void Constructor_SubscribesToIssuesChanged() => localHotspotsStore.Received().IssuesChanged += Arg.Any<EventHandler<IssuesChangedEventArgs>>();

    [TestMethod]
    public void Dispose_UnsubscribesFromIssuesChanged()
    {
        testSubject.Dispose();

        localHotspotsStore.Received().IssuesChanged -= Arg.Any<EventHandler<IssuesChangedEventArgs>>();
    }

    [TestMethod]
    public void GetHotspotsGroupViewModels_GroupsByFilePath()
    {
        var file1 = "file1.cs";
        var file2 = "file2.cs";
        MockHotspotsInStore(CreateMockedHotspot(file1), CreateMockedHotspot(file1), CreateMockedHotspot(file2));

        var groups = testSubject.GetHotspotsGroupViewModels();

        groups.Should().HaveCount(2);
        groups.Select(g => g.Title).Should().Contain([file1, file2]);
        groups.First(g => g.Title == file1).FilteredIssues.Should().HaveCount(2);
        groups.First(g => g.Title == file2).FilteredIssues.Should().ContainSingle();
    }

    [TestMethod]
    public void GetHotspotsGroupViewModels_TwoHotspotsInSameFile_CreatesOneGroupVmWithTwoIssues()
    {
        var path = "myFile.cs";
        var hotspot1 = CreateMockedHotspot(path);
        var hotspot2 = CreateMockedHotspot(path);
        MockHotspotsInStore(hotspot1, hotspot2);

        var groups = testSubject.GetHotspotsGroupViewModels();

        groups.Should().ContainSingle();
        VerifyExpectedHotspotGroupViewModel(groups[0] as GroupFileViewModel, hotspot1, hotspot2);
    }

    [TestMethod]
    public void GetHotspotsGroupViewModels_TwoHotspotsInDifferentFiles_CreatesTwoGroupsWithOneIssueEach()
    {
        var hotspot1 = CreateMockedHotspot("myFile.cs");
        var hotspot2 = CreateMockedHotspot("myFile.js");
        MockHotspotsInStore(hotspot1, hotspot2);

        var groups = testSubject.GetHotspotsGroupViewModels();

        groups.Should().HaveCount(2);
        VerifyExpectedHotspotGroupViewModel(groups[0] as GroupFileViewModel, hotspot1);
        VerifyExpectedHotspotGroupViewModel(groups[1] as GroupFileViewModel, hotspot2);
    }

    [TestMethod]
    public void HotspotsChanged_RaisedOnStoreIssuesChanged()
    {
        var raised = false;
        testSubject.HotspotsChanged += (_, _) => raised = true;

        localHotspotsStore.IssuesChanged += Raise.Event<EventHandler<IssuesChangedEventArgs>>(null, null);

        raised.Should().BeTrue();
    }

    private static LocalHotspot CreateMockedHotspot(string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var analysisIssueBase = Substitute.For<IAnalysisIssueBase>();
        analysisIssueBase.PrimaryLocation.FilePath.Returns(filePath);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);

        return new LocalHotspot(analysisIssueVisualization, default, default);
    }

    private void MockHotspotsInStore(params LocalHotspot[] hotspots) => localHotspotsStore.GetAllLocalHotspots().Returns(hotspots);

    private static void VerifyExpectedHotspotGroupViewModel(GroupFileViewModel groupFileVm, params LocalHotspot[] expectedHotspots)
    {
        groupFileVm.Should().NotBeNull();
        groupFileVm.FilePath.Should().Be(expectedHotspots[0].Visualization.Issue.PrimaryLocation.FilePath);
        groupFileVm.FilteredIssues.Should().HaveCount(expectedHotspots.Length);
        foreach (var expectedHotspot in expectedHotspots)
        {
            groupFileVm.FilteredIssues.Should().ContainSingle(vm => ((HotspotViewModel)vm).LocalHotspot == expectedHotspot);
        }
    }
}
