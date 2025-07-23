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

        dependencyRisksStore.DidNotReceiveWithAnyArgs().Set(default, default);
        converter.DidNotReceiveWithAnyArgs().Convert(default);
    }

    [TestMethod]
    public void DidChangeScaIssues_ConfigurationScopeMatches_UpdatesStoreWithConvertedIssues()
    {
        dependencyRisksStore.CurrentConfigurationScope.Returns(ConfigScopeId);
        var (scaIssue1, dependencyRisk1) = CreateScaIssueAndDependencyRisk();
        var (scaIssue2, dependencyRisk2) = CreateScaIssueAndDependencyRisk();
        var (scaIssue3, dependencyRisk3) = CreateScaIssueAndDependencyRisk();
        var parameters = CreateParams(ConfigScopeId, [scaIssue1, scaIssue2], [scaIssue3]);

        testSubject.DidChangeDependencyRisks(parameters);

        dependencyRisksStore.Received(1).Set(
            Arg.Is<IDependencyRisk[]>(issues =>
                issues.Contains(dependencyRisk1) &&
                issues.Contains(dependencyRisk2) &&
                issues.Contains(dependencyRisk3)),
            ConfigScopeId);
        logger.DidNotReceiveWithAnyArgs().WriteLine(default, default, default);
    }

    [TestMethod]
    public void DidChangeScaIssues_EmptyLists_UpdatesStoreWithEmptyList()
    {
        dependencyRisksStore.CurrentConfigurationScope.Returns(ConfigScopeId);

        var parameters = CreateParams(ConfigScopeId);

        testSubject.DidChangeDependencyRisks(parameters);

        converter.DidNotReceiveWithAnyArgs().Convert(default!);
        dependencyRisksStore.Received(1).Set(
            Arg.Is<IEnumerable<IDependencyRisk>>(issues => !issues.Any()),
            ConfigScopeId);
    }

    private (DependencyRiskDto scaIssue, IDependencyRisk dependencyRisk) CreateScaIssueAndDependencyRisk()
    {
        var scaIssue = new DependencyRiskDto(
            Guid.NewGuid(),
            default,
            default,
            default,
            string.Empty,
            string.Empty,
            []);
        var dependencyRisk = Substitute.For<IDependencyRisk>();
        converter.Convert(scaIssue).Returns(dependencyRisk);

        return (scaIssue, dependencyRisk);
    }

    private static DidChangeDependencyRisksParams CreateParams(
        string configScopeId,
        List<DependencyRiskDto> addedScaIssues = null,
        List<DependencyRiskDto> updatedScaIssues = null) =>
        new(
            configScopeId,
            [Guid.NewGuid()],
            addedScaIssues ?? [],
            updatedScaIssues ?? []);
}
