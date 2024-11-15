/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Issue;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.State;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Education.UnitTests.Rule;

[TestClass]
public class SLCoreRuleMetaDataProviderTests
{
    private readonly SonarCompositeRuleId compositeRuleId = new("rule", "key1");
    private readonly ConfigurationScope configurationScope = new("id");
    private readonly RuleInfo defaultRuleInfo = new(default, default, default, default, default, default, default, default);
    private readonly EffectiveIssueDetailsDto effectiveIssueDetailsDto = new(default, default, default, default, default, default, default, default);
    private readonly string errorMessage = "my message";
    private readonly Guid issueId = Guid.NewGuid();

    private IActiveConfigScopeTracker configScopeTrackerMock;
    private IIssueSLCoreService issueServiceMock;
    private TestLogger logger;
    private IRuleInfoConverter ruleInfoConverter;
    private IRulesSLCoreService rulesServiceMock;
    private ISLCoreServiceProvider serviceProviderMock;
    private SLCoreRuleMetaDataProvider testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        serviceProviderMock = Substitute.For<ISLCoreServiceProvider>();
        configScopeTrackerMock = Substitute.For<IActiveConfigScopeTracker>();
        issueServiceMock = Substitute.For<IIssueSLCoreService>();
        rulesServiceMock = Substitute.For<IRulesSLCoreService>();
        ruleInfoConverter = Substitute.For<IRuleInfoConverter>();
        logger = new TestLogger();

        testSubject = new SLCoreRuleMetaDataProvider(serviceProviderMock, configScopeTrackerMock, ruleInfoConverter, logger);
        MockupServices();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SLCoreRuleMetaDataProvider, IRuleMetaDataProvider>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IRuleInfoConverter>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SLCoreRuleMetaDataProvider>();

    [TestMethod]
    public async Task GetRuleInfoAsync_NoActiveScope_ReturnsNull()
    {
        SetUpConfigScopeTracker(null);

        var ruleInfo = await testSubject.GetRuleInfoAsync(compositeRuleId);

        ruleInfo.Should().BeNull();
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_ServiceUnavailable_ReturnsNull()
    {
        SetUpRuleServiceProvider(false);

        var ruleInfo = await testSubject.GetRuleInfoAsync(compositeRuleId);

        ruleInfo.Should().BeNull();
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void GetRuleInfoAsync_ServiceThrows_ReturnsNullAndLogs()
    {
        MockGetEffectiveRuleDetailsAsyncThrows();

        var act = () => testSubject.GetRuleInfoAsync(compositeRuleId);

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists(errorMessage);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_ForIssue_NoActiveScope_ReturnsNull()
    {
        SetUpConfigScopeTracker(null);

        var ruleInfo = await testSubject.GetRuleInfoAsync(compositeRuleId, issueId);

        ruleInfo.Should().BeNull();
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_ForIssue_IssueServiceUnavailable_ReturnsResultFromRulesService()
    {
        SetUpIssueServiceProvider(false);
        MockGetEffectiveRuleDetailsAsync(compositeRuleId.ToString(), configurationScope.Id);

        var ruleInfo = await testSubject.GetRuleInfoAsync(compositeRuleId, issueId);

        ruleInfo.Should().NotBeNull();
        logger.AssertNoOutputMessages();
        VerifyGetRuleDetailsWasCalled(compositeRuleId.ToString());
    }

    [TestMethod]
    public void GetRuleInfoAsync_ForIssue_IssueServiceThrows_ReturnsNullAndLogs()
    {
        MockGetEffectiveIssueDetailsAsyncThrows();

        var act = () => testSubject.GetRuleInfoAsync(compositeRuleId, issueId);

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists(errorMessage);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_IssueIdNull_CallsGetEffectiveRuleDetailsAsync()
    {
        await testSubject.GetRuleInfoAsync(compositeRuleId, null);

        VerifyGetRuleDetailsWasCalled(compositeRuleId.ToString());
        VerifyIssueDetailsWasNotCalled();
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_IssueIdNotNull_CallsGetEffectiveIssueDetailsAsync()
    {
        MockGetEffectiveIssueDetailsAsync(issueId, configurationScope.Id);

        await testSubject.GetRuleInfoAsync(compositeRuleId, issueId);

        VerifyRuleDetailsWasNotCalled();
        VerifyGetIssueDetailsWasCalled(issueId);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_GetEffectiveIssueDetailsAsyncThrows_CallsGetEffectiveRuleDetailsAsync()
    {
        MockGetEffectiveIssueDetailsAsyncThrows();

        await testSubject.GetRuleInfoAsync(compositeRuleId, issueId);

        VerifyGetRuleDetailsWasCalled(compositeRuleId.ToString());
        VerifyGetIssueDetailsWasCalled(issueId);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_BothServicesThrow_ReturnsNull()
    {
        MockGetEffectiveIssueDetailsAsyncThrows();
        MockGetEffectiveRuleDetailsAsyncThrows();

        var result = await testSubject.GetRuleInfoAsync(compositeRuleId, issueId);

        result.Should().BeNull();
        VerifyGetRuleDetailsWasCalled(compositeRuleId.ToString());
        VerifyGetIssueDetailsWasCalled(issueId);
    }

    private void SetUpConfigScopeTracker(ConfigurationScope scope) => configScopeTrackerMock.Current.Returns(scope);

    private void MockGetEffectiveIssueDetailsAsyncThrows() => issueServiceMock.GetEffectiveIssueDetailsAsync(Arg.Any<GetEffectiveIssueDetailsParams>()).ThrowsAsync(new Exception(errorMessage));

    private void MockGetEffectiveRuleDetailsAsyncThrows() => rulesServiceMock.GetEffectiveRuleDetailsAsync(Arg.Any<GetEffectiveRuleDetailsParams>()).ThrowsAsync(new Exception(errorMessage));

    private void MockGetEffectiveIssueDetailsAsync(Guid id, string configScopeId) =>
        issueServiceMock.GetEffectiveIssueDetailsAsync(Arg.Is<GetEffectiveIssueDetailsParams>(x => x.configurationScopeId == configScopeId && x.issueId == id))
            .Returns(new GetEffectiveIssueDetailsResponse(effectiveIssueDetailsDto));

    private void MockGetEffectiveRuleDetailsAsync(string ruleKey, string configScopeId) =>
        rulesServiceMock.GetEffectiveRuleDetailsAsync(Arg.Is<GetEffectiveRuleDetailsParams>(x => x.configurationScopeId == configScopeId && x.ruleKey == ruleKey))
            .Returns(new GetEffectiveRuleDetailsResponse(default));

    private void SetUpRuleServiceProvider(bool result) =>
        serviceProviderMock.TryGetTransientService(out Arg.Any<IRulesSLCoreService>()).Returns(callInfo =>
        {
            callInfo[0] = rulesServiceMock;
            return result;
        });

    private void SetUpIssueServiceProvider(bool result) =>
        serviceProviderMock.TryGetTransientService(out Arg.Any<IIssueSLCoreService>()).Returns(callInfo =>
        {
            callInfo[0] = issueServiceMock;
            return result;
        });

    private void MockupServices()
    {
        MockRuleInfoConverter();
        SetUpIssueServiceProvider(true);
        SetUpRuleServiceProvider(true);
        SetUpConfigScopeTracker(configurationScope);
    }

    private void MockRuleInfoConverter() => ruleInfoConverter.Convert(Arg.Any<IRuleDetails>()).Returns(defaultRuleInfo);

    private void VerifyGetIssueDetailsWasCalled(Guid id) => issueServiceMock.Received(1).GetEffectiveIssueDetailsAsync(Arg.Is<GetEffectiveIssueDetailsParams>(x => x.issueId == id));

    private void VerifyGetRuleDetailsWasCalled(string ruleKey) => rulesServiceMock.Received(1).GetEffectiveRuleDetailsAsync(Arg.Is<GetEffectiveRuleDetailsParams>(x => x.ruleKey == ruleKey));

    private void VerifyIssueDetailsWasNotCalled() => issueServiceMock.DidNotReceive().GetEffectiveIssueDetailsAsync(Arg.Any<GetEffectiveIssueDetailsParams>());

    private void VerifyRuleDetailsWasNotCalled() => rulesServiceMock.DidNotReceive().GetEffectiveRuleDetailsAsync(Arg.Any<GetEffectiveRuleDetailsParams>());
}
