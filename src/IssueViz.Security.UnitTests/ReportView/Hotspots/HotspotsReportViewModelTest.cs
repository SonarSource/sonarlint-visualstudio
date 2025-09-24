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

using System.Windows;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Hotspots;

[TestClass]
public class HotspotsReportViewModelTest
{
    private readonly LocalHotspot serverHotspot = CreateMockedHotspot("myFile.cs", "serverKey");
    private ILocalHotspotsStore localHotspotsStore;
    private IMessageBox messageBox;
    private IReviewHotspotsService reviewHotspotsService;
    private ITelemetryManager telemetryManager;
    private HotspotsReportViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        localHotspotsStore = Substitute.For<ILocalHotspotsStore>();
        reviewHotspotsService = Substitute.For<IReviewHotspotsService>();
        messageBox = Substitute.For<IMessageBox>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        testSubject = new HotspotsReportViewModel(localHotspotsStore, reviewHotspotsService, messageBox, telemetryManager);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<HotspotsReportViewModel, IHotspotsReportViewModel>(
            MefTestHelpers.CreateExport<ILocalHotspotsStore>(),
            MefTestHelpers.CreateExport<IReviewHotspotsService>(),
            MefTestHelpers.CreateExport<IMessageBox>(),
            MefTestHelpers.CreateExport<ITelemetryManager>()
        );

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
        groups.First(g => g.Title == file1).AllIssues.Should().HaveCount(2);
        groups.First(g => g.Title == file2).AllIssues.Should().ContainSingle();
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
        testSubject.IssuesChanged += (_, _) => raised = true;

        localHotspotsStore.IssuesChanged += Raise.Event<EventHandler<IssuesChangedEventArgs>>(null, null);

        raised.Should().BeTrue();
    }

    [TestMethod]
    public async Task ShowHotspotInBrowserAsync_CallsHandlerAndTelemetry()
    {
        var hotspot = CreateMockedHotspot("myFile.cs");

        await testSubject.ShowHotspotInBrowserAsync(hotspot);

        reviewHotspotsService.Received(1).OpenHotspotAsync(hotspot.Visualization.Issue.IssueServerKey).IgnoreAwaitForAssert();
        telemetryManager.Received(1).HotspotInvestigatedRemotely();
    }

    [TestMethod]
    public async Task GetAllowedStatusesAsync_ChangeStatusPermitted_ReturnsListOfAllowedStatuses()
    {
        var allowedStatuses = new List<HotspotStatus> { HotspotStatus.Fixed, HotspotStatus.ToReview };
        MockChangeStatusPermitted(serverHotspot.Visualization.Issue.IssueServerKey, allowedStatuses);

        var result = await testSubject.GetAllowedStatusesAsync(new HotspotViewModel(serverHotspot));

        result.Should().BeEquivalentTo(allowedStatuses);
        messageBox.DidNotReceive().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public async Task GetAllowedStatusesAsync_ChangeStatusNotPermitted_ShowsMessageBoxAndReturnsNull()
    {
        var reason = "Not permitted";
        MockChangeStatusNotPermitted(serverHotspot.Visualization.Issue.IssueServerKey, reason);

        var result = await testSubject.GetAllowedStatusesAsync(new HotspotViewModel(serverHotspot));

        result.Should().BeNull();
        messageBox.Received(1).Show(Arg.Is<string>(x => x == string.Format(Resources.ReviewHotspotWindow_CheckReviewPermittedFailureMessage, reason)),
            Arg.Is<string>(x => x == Resources.ReviewHotspotWindow_FailureTitle), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [TestMethod]
    public async Task GetAllowedStatusesAsync_NoStatusSelected_ShowsMessageBoxAndReturnsNull()
    {
        var result = await testSubject.GetAllowedStatusesAsync(null);

        result.Should().BeNull();
        messageBox.Received(1).Show(
            Arg.Is<string>(x => x == string.Format(Resources.ReviewHotspotWindow_CheckReviewPermittedFailureMessage, Resources.ReviewHotspotWindow_NoStatusSelectedFailureMessage)),
            Arg.Is<string>(x => x == Resources.ReviewHotspotWindow_FailureTitle), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [TestMethod]
    [DataRow(HotspotStatus.Fixed)]
    [DataRow(HotspotStatus.ToReview)]
    [DataRow(HotspotStatus.Acknowledged)]
    [DataRow(HotspotStatus.Safe)]
    public async Task ChangeHotspotStatusAsync_Succeeds_ReturnsTrue(HotspotStatus newStatus)
    {
        var hotspotViewModel = new HotspotViewModel(serverHotspot);
        MockReviewHotspot(serverHotspot.Visualization.Issue.IssueServerKey, newStatus, true);

        var result = await testSubject.ChangeHotspotStatusAsync(hotspotViewModel, newStatus);

        result.Should().BeTrue();
        reviewHotspotsService.Received(1).ReviewHotspotAsync(serverHotspot.Visualization.Issue.IssueServerKey, newStatus).IgnoreAwaitForAssert();
        messageBox.DidNotReceive().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    [DataRow(HotspotStatus.Fixed)]
    [DataRow(HotspotStatus.ToReview)]
    [DataRow(HotspotStatus.Acknowledged)]
    [DataRow(HotspotStatus.Safe)]
    public async Task ChangeHotspotStatusAsync_Fails_ShowsMessageBox(HotspotStatus newStatus)
    {
        var hotspotViewModel = new HotspotViewModel(serverHotspot);
        MockReviewHotspot(serverHotspot.Visualization.Issue.IssueServerKey, newStatus, false);

        var result = await testSubject.ChangeHotspotStatusAsync(hotspotViewModel, newStatus);

        result.Should().BeFalse();
        messageBox.Received(1).Show(Arg.Is<string>(x => x == Resources.ReviewHotspotWindow_ReviewFailureMessage), Arg.Is<string>(x => x == Resources.ReviewHotspotWindow_FailureTitle),
            MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static LocalHotspot CreateMockedHotspot(string filePath, string hotspotKey = null)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var analysisIssueBase = Substitute.For<IAnalysisIssueBase>();
        analysisIssueBase.PrimaryLocation.FilePath.Returns(filePath);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);
        analysisIssueVisualization.Issue.IssueServerKey.Returns(hotspotKey);

        return new LocalHotspot(analysisIssueVisualization, default, default);
    }

    private void MockHotspotsInStore(params LocalHotspot[] hotspots) => localHotspotsStore.GetAllLocalHotspots().Returns(hotspots);

    private static void VerifyExpectedHotspotGroupViewModel(GroupFileViewModel groupFileVm, params LocalHotspot[] expectedHotspots)
    {
        groupFileVm.Should().NotBeNull();
        groupFileVm.FilePath.Should().Be(expectedHotspots[0].Visualization.Issue.PrimaryLocation.FilePath);
        groupFileVm.AllIssues.Should().HaveCount(expectedHotspots.Length);
        foreach (var expectedHotspot in expectedHotspots)
        {
            groupFileVm.AllIssues.Should().ContainSingle(vm => ((HotspotViewModel)vm).LocalHotspot == expectedHotspot);
        }
    }

    private void MockChangeStatusPermitted(string hotspotKey, List<HotspotStatus> allowedStatuses) =>
        reviewHotspotsService.CheckReviewHotspotPermittedAsync(hotspotKey).Returns(new ReviewHotspotPermittedArgs(allowedStatuses));

    private void MockChangeStatusNotPermitted(string hotspotKey, string reason) =>
        reviewHotspotsService.CheckReviewHotspotPermittedAsync(hotspotKey).Returns(new ReviewHotspotNotPermittedArgs(reason));

    private void MockReviewHotspot(string hotspotKey, HotspotStatus newStatus, bool succeeded) => reviewHotspotsService.ReviewHotspotAsync(hotspotKey, newStatus).Returns(succeeded);
}
