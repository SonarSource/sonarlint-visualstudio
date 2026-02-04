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

using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues.ReviewIssue;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Issue;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Issues.ReviewIssue;

[TestClass]
public class ReviewIssuesServiceTests
{
    private ISLCoreServiceProvider serviceProvider;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IThreadHandling threadHandling;
    private TestLogger logger;
    private IIssueSLCoreService issueSlCoreService;
    private ReviewIssuesService testSubject;
    private const string IssueKey = "test-issue-key";
    private const string ConfigScopeId = "test-scope-id";
    private const string ConnectionId = "test-connection-id";
    private const string Comment = "test comment";

    [TestInitialize]
    public void TestInitialize()
    {
        serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        issueSlCoreService = Substitute.For<IIssueSLCoreService>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        logger = Substitute.ForPartsOf<TestLogger>();

        SetupConfigScope();
        SetupServiceProvider();

        testSubject = new ReviewIssuesService(
            activeConfigScopeTracker,
            serviceProvider,
            logger,
            threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ReviewIssuesService, IReviewIssuesService>(
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ReviewIssuesService>();

    [TestMethod]
    public async Task ReviewIssueAsync_ServiceProviderReturnsNull_ReturnsFalse()
    {
        serviceProvider.TryGetTransientService(out Arg.Any<IIssueSLCoreService>()).Returns(false);

        var result = await testSubject.ReviewIssueAsync(IssueKey, ResolutionStatus.ACCEPT, Comment);

        result.Should().BeFalse();
        logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public async Task ReviewIssueAsync_WithComment_CallsChangeStatusAndAddComment()
    {
        var result = await testSubject.ReviewIssueAsync(IssueKey, ResolutionStatus.ACCEPT, Comment);

        result.Should().BeTrue();
        await issueSlCoreService.Received(1).ChangeStatusAsync(Arg.Is<ChangeIssueStatusParams>(p =>
            p.configurationScopeId == ConfigScopeId &&
            p.issueKey == IssueKey &&
            p.newStatus == ResolutionStatus.ACCEPT &&
            p.isTaintIssue == false));
        await issueSlCoreService.Received(1).AddCommentAsync(Arg.Is<AddIssueCommentParams>(p =>
            p.configurationScopeId == ConfigScopeId &&
            p.issueKey == IssueKey &&
            p.text == Comment));
    }

    [TestMethod]
    public async Task ReviewIssueAsync_WithoutComment_CallsOnlyChangeStatus()
    {
        var result = await testSubject.ReviewIssueAsync(IssueKey, ResolutionStatus.ACCEPT, null);

        result.Should().BeTrue();
        await issueSlCoreService.Received(1).ChangeStatusAsync(Arg.Any<ChangeIssueStatusParams>());
        await issueSlCoreService.DidNotReceive().AddCommentAsync(Arg.Any<AddIssueCommentParams>());
    }

    [TestMethod]
    public async Task ReviewIssueAsync_WithEmptyComment_CallsOnlyChangeStatus()
    {
        var result = await testSubject.ReviewIssueAsync(IssueKey, ResolutionStatus.ACCEPT, "   ");

        result.Should().BeTrue();
        await issueSlCoreService.Received(1).ChangeStatusAsync(Arg.Any<ChangeIssueStatusParams>());
        await issueSlCoreService.DidNotReceive().AddCommentAsync(Arg.Any<AddIssueCommentParams>());
    }

    [TestMethod]
    public async Task ReviewIssueAsync_WithTaintFlag_PassesCorrectly()
    {
        var result = await testSubject.ReviewIssueAsync(IssueKey, ResolutionStatus.WONT_FIX, Comment, isTaint: true);

        result.Should().BeTrue();
        await issueSlCoreService.Received(1).ChangeStatusAsync(Arg.Is<ChangeIssueStatusParams>(p =>
            p.isTaintIssue == true));
    }

    [TestMethod]
    public async Task ReviewIssueAsync_ChangeStatusThrows_ReturnsFalseAndLogs()
    {
        issueSlCoreService.ChangeStatusAsync(Arg.Any<ChangeIssueStatusParams>()).Throws(new Exception("test error"));

        var result = await testSubject.ReviewIssueAsync(IssueKey, ResolutionStatus.ACCEPT, Comment);

        result.Should().BeFalse();
        logger.AssertPartialOutputStringExists(string.Format(Resources.ReviewIssueService_AnErrorOccurred, "test error"));
    }

    [TestMethod]
    public async Task ReopenIssueAsync_ServiceProviderReturnsNull_ReturnsFalse()
    {
        serviceProvider.TryGetTransientService(out Arg.Any<IIssueSLCoreService>()).Returns(false);

        var result = await testSubject.ReopenIssueAsync(IssueKey);

        result.Should().BeFalse();
        logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public async Task ReopenIssueAsync_Success_ReturnsTrue()
    {
        issueSlCoreService.ReopenIssueAsync(Arg.Any<ReopenIssueParams>())
            .Returns(new ReopenIssueResponse(true));

        var result = await testSubject.ReopenIssueAsync(IssueKey);

        result.Should().BeTrue();
        await issueSlCoreService.Received(1).ReopenIssueAsync(Arg.Is<ReopenIssueParams>(p =>
            p.configurationScopeId == ConfigScopeId &&
            p.issueId == IssueKey &&
            p.isTaintIssue == false));
    }

    [TestMethod]
    public async Task ReopenIssueAsync_Failure_ReturnsFalse()
    {
        issueSlCoreService.ReopenIssueAsync(Arg.Any<ReopenIssueParams>())
            .Returns(new ReopenIssueResponse(false));

        var result = await testSubject.ReopenIssueAsync(IssueKey);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task ReopenIssueAsync_WithTaintFlag_PassesCorrectly()
    {
        issueSlCoreService.ReopenIssueAsync(Arg.Any<ReopenIssueParams>())
            .Returns(new ReopenIssueResponse(true));

        var result = await testSubject.ReopenIssueAsync(IssueKey, isTaint: true);

        result.Should().BeTrue();
        await issueSlCoreService.Received(1).ReopenIssueAsync(Arg.Is<ReopenIssueParams>(p =>
            p.isTaintIssue == true));
    }

    [TestMethod]
    public async Task ReopenIssueAsync_Throws_ReturnsFalseAndLogs()
    {
        issueSlCoreService.ReopenIssueAsync(Arg.Any<ReopenIssueParams>()).Throws(new Exception("test error"));

        var result = await testSubject.ReopenIssueAsync(IssueKey);

        result.Should().BeFalse();
        logger.AssertPartialOutputStringExists(string.Format(Resources.ReviewIssueService_AnErrorOccurred, "test error"));
    }

    [TestMethod]
    public async Task CheckReviewIssuePermittedAsync_ServiceProviderReturnsNull_ReturnsNotPermitted()
    {
        serviceProvider.TryGetTransientService(out Arg.Any<IIssueSLCoreService>()).Returns(false);

        var result = await testSubject.CheckReviewIssuePermittedAsync(IssueKey);

        result.Should().BeOfType<ReviewIssueNotPermittedArgs>();
        ((ReviewIssueNotPermittedArgs)result).Reason.Should().Be(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public async Task CheckReviewIssuePermittedAsync_Permitted_ReturnsPermittedWithStatuses()
    {
        var allowedStatuses = new List<ResolutionStatus> { ResolutionStatus.ACCEPT, ResolutionStatus.FALSE_POSITIVE };
        issueSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>())
            .Returns(new CheckStatusChangePermittedResponse(true, null, allowedStatuses));

        var result = await testSubject.CheckReviewIssuePermittedAsync(IssueKey);

        result.Should().BeOfType<ReviewIssuePermittedArgs>();
        ((ReviewIssuePermittedArgs)result).AllowedStatuses.Should().BeEquivalentTo(allowedStatuses);
        await issueSlCoreService.Received(1).CheckStatusChangePermittedAsync(Arg.Is<CheckStatusChangePermittedParams>(p =>
            p.connectionId == ConnectionId &&
            p.issueKey == IssueKey));
    }

    [TestMethod]
    public async Task CheckReviewIssuePermittedAsync_NotPermitted_ReturnsNotPermittedWithReason()
    {
        const string reason = "test reason";
        issueSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>())
            .Returns(new CheckStatusChangePermittedResponse(false, reason, null));

        var result = await testSubject.CheckReviewIssuePermittedAsync(IssueKey);

        result.Should().BeOfType<ReviewIssueNotPermittedArgs>();
        ((ReviewIssueNotPermittedArgs)result).Reason.Should().Be(reason);
        logger.AssertPartialOutputStringExists(string.Format(Resources.ReviewIssueService_NotPermitted, reason));
    }

    [TestMethod]
    public async Task CheckReviewIssuePermittedAsync_Throws_ReturnsNotPermittedAndLogs()
    {
        issueSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>())
            .Throws(new Exception("test error"));

        var result = await testSubject.CheckReviewIssuePermittedAsync(IssueKey);

        result.Should().BeOfType<ReviewIssueNotPermittedArgs>();
        ((ReviewIssueNotPermittedArgs)result).Reason.Should().Be("test error");
        logger.AssertPartialOutputStringExists(string.Format(Resources.ReviewIssueService_AnErrorOccurred, "test error"));
    }

    private void SetupConfigScope()
    {
        var configScope = new ConfigurationScope(ConfigScopeId, ConnectionId: ConnectionId);
        activeConfigScopeTracker.Current.Returns(configScope);
    }

    private void SetupServiceProvider()
    {
        serviceProvider.TryGetTransientService(out Arg.Any<IIssueSLCoreService>())
            .Returns(x =>
            {
                x[0] = issueSlCoreService;
                return true;
            });
    }
}
