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

using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Hotspot;
using SonarLint.VisualStudio.TestInfrastructure;
using CheckStatusChangePermittedParams = SonarLint.VisualStudio.SLCore.Service.Hotspot.CheckStatusChangePermittedParams;
using CheckStatusChangePermittedResponse = SonarLint.VisualStudio.SLCore.Service.Hotspot.CheckStatusChangePermittedResponse;
using SlCoreHotspotStatus = SonarLint.VisualStudio.SLCore.Common.Models.HotspotStatus;
using CoreHotspotStatus = SonarLint.VisualStudio.Core.Analysis.HotspotStatus;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.ReviewHotspot;

[TestClass]
public class ReviewHotspotsServiceTest
{
    private ReviewHotspotsService testSubject;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private TestLogger logger;
    private NoOpThreadHandler threadHandling;
    private IHotspotSlCoreService hotspotSlCoreService;
    private readonly CheckStatusChangePermittedResponse notPermittedResponse = new(permitted: false, notPermittedReason: "Some reason", allowedStatuses: []);
    private const string HotspotKey = "hotspotKey";

    [TestInitialize]
    public void TestInitialize()
    {
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        logger = Substitute.ForPartsOf<TestLogger>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        hotspotSlCoreService = Substitute.For<IHotspotSlCoreService>();
        testSubject = new ReviewHotspotsService(activeConfigScopeTracker, slCoreServiceProvider, logger, threadHandling);

        activeConfigScopeTracker.Current.Returns(new ConfigurationScope("CONFIG_SCOPE_ID", RootPath: "C:\\", ConnectionId: "CONNECTION_ID"));
        slCoreServiceProvider.TryGetTransientService(out IHotspotSlCoreService _).Returns(call =>
        {
            call[0] = hotspotSlCoreService;
            return true;
        });
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ReviewHotspotsService, IReviewHotspotsService>(
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ReviewHotspotsService>();

    [TestMethod]
    public void Logger_HasCorrectContext()
    {
        var substituteLogger = Substitute.For<ILogger>();

        _ = new ReviewHotspotsService(activeConfigScopeTracker, slCoreServiceProvider, substituteLogger, threadHandling);

        substituteLogger.Received(1).ForContext(nameof(ReviewHotspotsService));
    }

    [TestMethod]
    public async Task CheckReviewHotspotPermittedAsync_RunsOnBackgroundThread()
    {
        MockCheckStatusChangePermitted([SlCoreHotspotStatus.FIXED, SlCoreHotspotStatus.SAFE]);

        await testSubject.CheckReviewHotspotPermittedAsync(HotspotKey);

        await threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<IReviewHotspotPermissionArgs>>>());
    }

    [TestMethod]
    public async Task CheckReviewHotspotPermittedAsync_WhenServiceProviderNotInitialized_LogsAndReturnsNotPermitted()
    {
        ServiceProviderNotInitialized();

        var result = await testSubject.CheckReviewHotspotPermittedAsync(HotspotKey);

        result.Should().BeOfType<ReviewHotspotNotPermittedArgs>();
        logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public async Task CheckReviewHotspotPermittedAsync_SlCoreThrowsNoCriticalException_LogsAndReturnsFalse()
    {
        hotspotSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).ThrowsAsync(_ => throw new Exception("Some error"));

        var result = await testSubject.CheckReviewHotspotPermittedAsync(HotspotKey);

        result.Should().BeOfType<ReviewHotspotNotPermittedArgs>();
        logger.AssertPartialOutputStringExists(HotspotKey);
        logger.AssertPartialOutputStrings(string.Format(Resources.ReviewHotspotService_AnErrorOccurred, "Some error"));
    }

    [TestMethod]
    public void CheckReviewHotspotPermittedAsync_SlCoreThrowsCriticalException_Throws()
    {
        hotspotSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).ThrowsAsync(_ => throw new StackOverflowException());

        var act = () => testSubject.CheckReviewHotspotPermittedAsync(HotspotKey);

        act.Should().Throw<StackOverflowException>();
    }

    [TestMethod]
    public async Task CheckReviewHotspotPermittedAsync_WhenNotPermitted_LogsAndReturnsNotPermitted()
    {
        hotspotSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).Returns(notPermittedResponse);

        var result = await testSubject.CheckReviewHotspotPermittedAsync(HotspotKey);

        result.Should().BeOfType<ReviewHotspotNotPermittedArgs>();
        ((ReviewHotspotNotPermittedArgs)result).Reason.Should().Be(notPermittedResponse.notPermittedReason);
        logger.AssertPartialOutputStringExists(HotspotKey);
        logger.AssertPartialOutputStrings(string.Format(Resources.ReviewHotspotService_NotPermitted, "Some reason"));
    }

    [TestMethod]
    public async Task CheckReviewHotspotPermittedAsync_GetsAllowedStatuses()
    {
        MockCheckStatusChangePermitted([SlCoreHotspotStatus.FIXED, SlCoreHotspotStatus.SAFE]);

        var result = await testSubject.CheckReviewHotspotPermittedAsync(HotspotKey);

        result.Should().BeOfType<ReviewHotspotPermittedArgs>();
        ((ReviewHotspotPermittedArgs)result).AllowedStatuses.Should().BeEquivalentTo([CoreHotspotStatus.Fixed, CoreHotspotStatus.Safe]);
        await hotspotSlCoreService.Received().CheckStatusChangePermittedAsync(Arg.Is<CheckStatusChangePermittedParams>(x =>
            x.connectionId == activeConfigScopeTracker.Current.ConnectionId
            && x.hotspotKey == HotspotKey));
    }

    [TestMethod]
    public async Task ReviewHotspotAsync_RunsOnBackgroundThread()
    {
        await testSubject.ReviewHotspotAsync(HotspotKey, default);

        await threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<bool>>>());
    }

    [TestMethod]
    public async Task ReviewHotspotAsync_WhenServiceProviderNotInitialized_LogsAndReturnsFalse()
    {
        ServiceProviderNotInitialized();

        var result = await testSubject.ReviewHotspotAsync(HotspotKey, default);

        result.Should().BeFalse();
        logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public async Task ReviewHotspotAsync_WhenReviewHotspotThrowsNoCriticalException_LogsAndReturnFalse()
    {
        hotspotSlCoreService.ChangeStatusAsync(Arg.Any<ChangeHotspotStatusParams>()).Returns(call => throw new Exception("Some error"));

        var result = await testSubject.ReviewHotspotAsync(HotspotKey, default);

        result.Should().BeFalse();
        logger.AssertPartialOutputStringExists(HotspotKey);
        logger.AssertPartialOutputStrings(string.Format(Resources.ReviewHotspotService_AnErrorOccurred, "Some error"));
    }

    [TestMethod]
    public void ReviewHotspotAsync_WhenReviewHotspotThrowsCriticalException_Throws()
    {
        hotspotSlCoreService.ChangeStatusAsync(Arg.Any<ChangeHotspotStatusParams>()).Returns(call => throw new StackOverflowException());

        var act = () => testSubject.ReviewHotspotAsync(HotspotKey, default);

        act.Should().Throw<StackOverflowException>();
    }

    [TestMethod]
    [DataRow(SlCoreHotspotStatus.TO_REVIEW, CoreHotspotStatus.ToReview)]
    [DataRow(SlCoreHotspotStatus.ACKNOWLEDGED, CoreHotspotStatus.Acknowledged)]
    [DataRow(SlCoreHotspotStatus.FIXED, CoreHotspotStatus.Fixed)]
    [DataRow(SlCoreHotspotStatus.SAFE, CoreHotspotStatus.Safe)]
    public async Task ReviewHotspotAsync_ShouldChangeHotspotStatus(SlCoreHotspotStatus slCoreStatus, CoreHotspotStatus newStatus)
    {
        var result = await testSubject.ReviewHotspotAsync(HotspotKey, newStatus);

        result.Should().BeTrue();
        await hotspotSlCoreService.Received().ChangeStatusAsync(Arg.Is<ChangeHotspotStatusParams>(x =>
            x.hotspotKey == HotspotKey
            && x.newStatus == slCoreStatus
            && x.configurationScopeId == activeConfigScopeTracker.Current.Id));
    }

    private void ServiceProviderNotInitialized() => slCoreServiceProvider.TryGetTransientService(out Arg.Any<IHotspotSlCoreService>()).ReturnsForAnyArgs(false);

    private void MockCheckStatusChangePermitted(List<SlCoreHotspotStatus> allowedStatuses)
    {
        var permittedResponse = new CheckStatusChangePermittedResponse(permitted: true, notPermittedReason: null, allowedStatuses: allowedStatuses);
        hotspotSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).Returns(permittedResponse);
    }
}
