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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.DependencyRisks;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class DependencyRisksListenerTests
{
    private DependencyRisksListener testSubject;
    private IDependencyRisksStore dependencyRisksStore;
    private IScaIssueDtoToDependencyRiskConverter converter;
    private ILogger logger;

    private const string StoreConfigScopeId = "store-config-scope-id";
    private const string ParamsConfigScopeId = "params-config-scope-id";
    private const string ConfigScopeId = "config-scope-id";

    [TestInitialize]
    public void TestInitialize()
    {
        dependencyRisksStore = Substitute.For<IDependencyRisksStore>();
        converter = Substitute.For<IScaIssueDtoToDependencyRiskConverter>();
        logger = Substitute.For<ILogger>();

        testSubject = new DependencyRisksListener(dependencyRisksStore, converter, logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<DependencyRisksListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<IDependencyRisksStore>(),
            MefTestHelpers.CreateExport<IScaIssueDtoToDependencyRiskConverter>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<DependencyRisksListener>();

    [TestMethod]
    public void DidChangeScaIssues_ConfigurationScopeMismatch_LogsWarningAndDoesNotUpdateStore()
    {
        dependencyRisksStore.CurrentConfigurationScope.Returns(StoreConfigScopeId);
        var parameters = CreateParams(ParamsConfigScopeId);

        testSubject.DidChangeDependencyRisks(parameters);

        dependencyRisksStore.DidNotReceiveWithAnyArgs().Update(default);
        converter.DidNotReceiveWithAnyArgs().Convert(default);
    }

    [TestMethod]
    public void DidChangeScaIssues_ConfigurationScopeMatches_UpdatesStoreWithConvertedIssues()
    {
        dependencyRisksStore.CurrentConfigurationScope.Returns(ConfigScopeId);
        var (riskDto1, dependencyRisk1) = CreateScaIssueAndDependencyRisk();
        var (riskDto2, dependencyRisk2) = CreateScaIssueAndDependencyRisk();
        var (riskDto3, dependencyRisk3) = CreateScaIssueAndDependencyRisk();
        var closedRiskIds = new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var parameters = CreateParams(ConfigScopeId, [riskDto1, riskDto2], [riskDto3], closedRiskIds);

        testSubject.DidChangeDependencyRisks(parameters);

        var expectedUpdate = new DependencyRisksUpdate(
            ConfigScopeId,
            [dependencyRisk1, dependencyRisk2],
            [dependencyRisk3],
            closedRiskIds);

        VerifyUpdateCall(expectedUpdate);
        logger.DidNotReceiveWithAnyArgs().WriteLine(default, default, default);
    }

    [TestMethod]
    public void DidChangeScaIssues_EmptyLists_UpdatesStoreWithEmptyList()
    {
        dependencyRisksStore.CurrentConfigurationScope.Returns(ConfigScopeId);
        var parameters = CreateParams(ConfigScopeId);
        var expectedUpdate = new DependencyRisksUpdate(ConfigScopeId, [], [], []);

        testSubject.DidChangeDependencyRisks(parameters);

        converter.DidNotReceiveWithAnyArgs().Convert(default!);
        VerifyUpdateCall(expectedUpdate);
    }

    private void VerifyUpdateCall(DependencyRisksUpdate expectedUpdate)
    {
        dependencyRisksStore.Received(1).Update(Arg.Any<DependencyRisksUpdate>());
        var updateCall = dependencyRisksStore.ReceivedCalls().Single(call => call.GetMethodInfo().Name == "Update");
        var actualUpdate = (DependencyRisksUpdate)updateCall.GetArguments()[0];
        actualUpdate.Should().BeEquivalentTo(expectedUpdate);
    }

    private (DependencyRiskDto riskDto, IDependencyRisk dependencyRisk) CreateScaIssueAndDependencyRisk()
    {
        var riskDto = new DependencyRiskDto(
            Guid.NewGuid(),
            default,
            default,
            default,
            string.Empty,
            string.Empty,
            default,
            default,
            []);
        var dependencyRisk = Substitute.For<IDependencyRisk>();
        converter.Convert(riskDto).Returns(dependencyRisk);

        return (riskDto, dependencyRisk);
    }

    private static DidChangeDependencyRisksParams CreateParams(
        string configScopeId,
        List<DependencyRiskDto> addedScaIssues = null,
        List<DependencyRiskDto> updatedScaIssues = null,
        HashSet<Guid> closedDependencyRiskIds = null) =>
        new(
            configScopeId,
            closedDependencyRiskIds ?? [],
            addedScaIssues ?? [],
            updatedScaIssues ?? []);
}
