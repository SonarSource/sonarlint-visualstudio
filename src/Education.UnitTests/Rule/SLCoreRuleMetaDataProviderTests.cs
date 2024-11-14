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

using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Rules;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.State;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.SLCore.Service.Issue;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;

namespace SonarLint.VisualStudio.Education.UnitTests.Rule;

[TestClass]
public class SLCoreRuleMetaDataProviderTests
{
    private static readonly SonarCompositeRuleId CompositeRuleId = new("rule", "key1");

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
        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out var logger);
        SetUpServiceProvider(serviceProviderMock, out _);
        SetUpConfigScopeTracker(configScopeTrackerMock, null);

        var ruleInfo = await testSubject.GetRuleInfoAsync(CompositeRuleId);

        ruleInfo.Should().BeNull();
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_ServiceUnavailable_ReturnsNull()
    {
        var testSubject = CreateTestSubject(out _, out var configScopeTrackerMock, out var logger);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope("id"));

        var ruleInfo = await testSubject.GetRuleInfoAsync(CompositeRuleId);

        ruleInfo.Should().BeNull();
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void GetRuleInfoAsync_ServiceThrows_ReturnsNullAndLogs()
    {
        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out var logger);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope("id"));
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        rulesServiceMock
            .Setup(x => x.GetEffectiveRuleDetailsAsync(It.IsAny<GetEffectiveRuleDetailsParams>()))
            .ThrowsAsync(new Exception("my message"));

        var act = () => testSubject.GetRuleInfoAsync(CompositeRuleId);

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists("my message");
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_ForIssue_NoActiveScope_ReturnsNull()
    {
        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out var logger);
        SetUpIssueServiceProvider(serviceProviderMock, out _);
        SetUpConfigScopeTracker(configScopeTrackerMock, null);
        var issueId = Guid.NewGuid();

        var ruleInfo = await testSubject.GetRuleInfoAsync(default,issueId);

        ruleInfo.Should().BeNull();
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_ForIssue_ServiceUnavailable_ReturnsNull()
    {
        var testSubject = CreateTestSubject(out _, out var configScopeTrackerMock, out var logger);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope("id"));

        var ruleInfo = await testSubject.GetRuleInfoAsync(default,Guid.NewGuid());

        ruleInfo.Should().BeNull();
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void GetRuleInfoAsync_ForIssue_ServiceThrows_ReturnsNullAndLogs()
    {
        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out var logger);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope("id"));
        SetUpIssueServiceProvider(serviceProviderMock, out var issueServiceMock);
        issueServiceMock
            .Setup(x => x.GetEffectiveIssueDetailsAsync(It.IsAny<GetEffectiveIssueDetailsParams>()))
            .ThrowsAsync(new Exception("my message"));

        var act = () => testSubject.GetRuleInfoAsync(default,Guid.NewGuid());

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists("my message");
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_FilterableIssueNull_CallsGetEffectiveRuleDetailsAsync()
    {
        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out _);
        SetUpIssueServiceProvider(serviceProviderMock, out var issueServiceMock);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope("configscope"));

        await testSubject.GetRuleInfoAsync(CompositeRuleId, null);

        rulesServiceMock.Verify(x => x.GetEffectiveRuleDetailsAsync(It.Is<GetEffectiveRuleDetailsParams>(p => p.ruleKey == CompositeRuleId.ToString())), Times.Once);
        issueServiceMock.Verify(x => x.GetEffectiveIssueDetailsAsync(It.IsAny<GetEffectiveIssueDetailsParams>()), Times.Never);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_FilterableIssueIdNull_CallsGetEffectiveRuleDetailsAsync()
    {
        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out _);
        SetUpIssueServiceProvider(serviceProviderMock, out var issueServiceMock);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope("configscope"));
        Guid? issueId = null;

        await testSubject.GetRuleInfoAsync(CompositeRuleId, issueId);

        rulesServiceMock.Verify(x => x.GetEffectiveRuleDetailsAsync(It.Is<GetEffectiveRuleDetailsParams>(p => p.ruleKey == CompositeRuleId.ToString())), Times.Once);
        issueServiceMock.Verify(x => x.GetEffectiveIssueDetailsAsync(It.IsAny<GetEffectiveIssueDetailsParams>()), Times.Never);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_FilterableIssueIdNotNull_CallsGetEffectiveIssueDetailsAsync()
    {
        var configScopeId = "configscope";
        var issueId = Guid.NewGuid();
        var testSubject = CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out _);
        SetUpIssueServiceProvider(serviceProviderMock, out var issueServiceMock);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope(configScopeId));
        SetupIssuesService(issueServiceMock, issueId, configScopeId, CreateEffectiveIssueDetailsDto(new MQRModeDetails(default, default)));

        await testSubject.GetRuleInfoAsync(CompositeRuleId, issueId);

        rulesServiceMock.Verify(x => x.GetEffectiveRuleDetailsAsync(It.IsAny<GetEffectiveRuleDetailsParams>()), Times.Never);
        issueServiceMock.Verify(x => x.GetEffectiveIssueDetailsAsync(It.Is<GetEffectiveIssueDetailsParams>(p => p.issueId == issueId)), Times.Once);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_GetEffectiveIssueDetailsAsyncThrows_CallsGetEffectiveRuleDetailsAsync()
    {
        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out _);
        SetUpIssueServiceProvider(serviceProviderMock, out var issueServiceMock);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope("configscope"));
        var issueId = Guid.NewGuid();
        issueServiceMock
            .Setup(x => x.GetEffectiveIssueDetailsAsync(It.IsAny<GetEffectiveIssueDetailsParams>()))
            .ThrowsAsync(new Exception("my message"));

        await testSubject.GetRuleInfoAsync(CompositeRuleId, issueId);

        rulesServiceMock.Verify(x => x.GetEffectiveRuleDetailsAsync(It.Is<GetEffectiveRuleDetailsParams>(p => p.ruleKey == CompositeRuleId.ToString())), Times.Once);
        issueServiceMock.Verify(x => x.GetEffectiveIssueDetailsAsync(It.Is<GetEffectiveIssueDetailsParams>(p => p.issueId == issueId)), Times.Once);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_BothServicesThrow_ReturnsNull()
    {
        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out _);
        SetUpIssueServiceProvider(serviceProviderMock, out var issueServiceMock);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope("configscope"));
        var issueId = Guid.NewGuid();
        issueServiceMock
            .Setup(x => x.GetEffectiveIssueDetailsAsync(It.IsAny<GetEffectiveIssueDetailsParams>()))
            .ThrowsAsync(new Exception("my message"));
        rulesServiceMock
            .Setup(x => x.GetEffectiveRuleDetailsAsync(It.IsAny<GetEffectiveRuleDetailsParams>()))
            .ThrowsAsync(new Exception("my message"));

        var result = await testSubject.GetRuleInfoAsync(CompositeRuleId, issueId);

        result.Should().BeNull();
        rulesServiceMock.Verify(x => x.GetEffectiveRuleDetailsAsync(It.Is<GetEffectiveRuleDetailsParams>(p => p.ruleKey == CompositeRuleId.ToString())), Times.Once);
        issueServiceMock.Verify(x => x.GetEffectiveIssueDetailsAsync(It.Is<GetEffectiveIssueDetailsParams>(p => p.issueId == issueId)), Times.Once);
    }

    private static void SetUpConfigScopeTracker(
        Mock<IActiveConfigScopeTracker> configScopeTrackerMock,
        ConfigurationScope scope) =>
        configScopeTrackerMock.SetupGet(x => x.Current).Returns(scope);

    private static void SetupIssuesService(
        Mock<IIssueSLCoreService> issuesServiceMock,
        Guid id,
        string configScopeId,
        EffectiveIssueDetailsDto response) =>
        issuesServiceMock
            .Setup(r => r.GetEffectiveIssueDetailsAsync(It.Is<GetEffectiveIssueDetailsParams>(p => p.configurationScopeId == configScopeId && p.issueId == id)))
            .ReturnsAsync(new GetEffectiveIssueDetailsResponse(response));

    private static void SetUpServiceProvider(
        Mock<ISLCoreServiceProvider> serviceProviderMock,
        out Mock<IRulesSLCoreService> rulesServiceMock)
    {
        rulesServiceMock = new Mock<IRulesSLCoreService>();
        var rulesService = rulesServiceMock.Object;
        serviceProviderMock.Setup(x => x.TryGetTransientService(out rulesService)).Returns(true);
    }

    private static void SetUpIssueServiceProvider(
        Mock<ISLCoreServiceProvider> serviceProviderMock,
        out Mock<IIssueSLCoreService> rulesServiceMock)
    {
        rulesServiceMock = new Mock<IIssueSLCoreService>();
        var rulesService = rulesServiceMock.Object;
        serviceProviderMock.Setup(x => x.TryGetTransientService(out rulesService)).Returns(true);
    }

    private static SLCoreRuleMetaDataProvider CreateTestSubject(
        out Mock<ISLCoreServiceProvider> serviceProviderMock,
        out Mock<IActiveConfigScopeTracker> configScopeTrackerMock,
        out TestLogger logger)
    {
        serviceProviderMock = new Mock<ISLCoreServiceProvider>();
        configScopeTrackerMock = new Mock<IActiveConfigScopeTracker>();
        configScopeTrackerMock = new Mock<IActiveConfigScopeTracker>();
        var ruleInfoConverter = new Mock<IRuleInfoConverter>();
        ruleInfoConverter.Setup(x => x.Convert(It.IsAny<IRuleDetails>())).Returns(new RuleInfo(default, default, default, default, default, default, default, default));
        logger = new TestLogger();
        return new SLCoreRuleMetaDataProvider(serviceProviderMock.Object, configScopeTrackerMock.Object, ruleInfoConverter.Object, logger);
    }

    private static EffectiveIssueDetailsDto CreateEffectiveIssueDetailsDto(Either<StandardModeDetails, MQRModeDetails> severityDetails,
        Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto> description = default) =>
        new(
            default,
            default,
            default,
            default,
            description,
            default,
            severityDetails, 
            default);
}
