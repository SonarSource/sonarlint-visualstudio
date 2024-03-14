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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Vsix.SLCore;
using SonarLint.VisualStudio.Integration.Vsix.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using SonarLint.VisualStudio.SLCore.State;
using SonarLint.VisualStudio.TestInfrastructure;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests.SLCore;

[TestClass]
public class SLCoreHandleTests
{
    private const string StorageRoot = "storageRootSl";
    private const string WorkDir = "workDirSl";
    private const string UserHome = "userHomeSl";
    
    private static readonly ClientConstantsDto ClientConstants = new(default, default);
    private static readonly FeatureFlagsDto FeatureFlags = new(default, default, default, default, default, default, default);
    private static readonly TelemetryClientConstantAttributesDto TelemetryConstants = new(default, default, default, default, default);

    private static readonly SonarQubeConnectionConfigurationDto SonarQubeConnection1 = new("sq1", true, "http://localhost/");
    private static readonly SonarQubeConnectionConfigurationDto SonarQubeConnection2 = new("sq2", true, "https://next.sonarqube.org/");
    private static readonly SonarCloudConnectionConfigurationDto SonarCloudConnection = new("sc", true, "https://sonarcloud.io/");

    private static readonly BoundSonarQubeProject Binding = new();
    
    private static readonly List<string> JarList = new() { "jar1" };

    [TestMethod]
    public async Task Initialize_ThrowsIfServicesUnavailable()
    {
        var testSubject = CreateTestSubject(out var slCoreRpcFactory, out _, out _, out _, out _, out _, out _, out _);
        SetUpSLCoreRpcFactory(slCoreRpcFactory, out var rpc);
        SetUpSLCoreRpc(rpc, out var serviceProvider);
        serviceProvider.TryGetTransientService(out Arg.Any<AnySLCoreService>()).ReturnsForAnyArgs(false);

        var act = () => testSubject.InitializeAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public async Task Initialize_SuccessfullyInitializesInCorrectOrder()
    {
        var testSubject = CreateTestSubject(out var slCoreRpcFactory,
            out var constantsProvider,
            out var foldersProvider,
            out var connectionsProvider,
            out var jarLocator,
            out var activeSolutionBoundTracker,
            out var configScopeUpdater,
            out var threadHandling);

        SetUpSuccessfulInitialization(slCoreRpcFactory,
            constantsProvider,
            foldersProvider,
            connectionsProvider,
            jarLocator,
            activeSolutionBoundTracker,
            out var lifecycleManagement,
            out var telemetryService);

        await testSubject.InitializeAsync();

        Received.InOrder(() =>
        {
            threadHandling.ThrowIfOnUIThread();
            slCoreRpcFactory.StartNewRpcInstance();
            lifecycleManagement.InitializeAsync(Arg.Is<InitializeParams>(parameters =>
                parameters.clientConstantInfo == ClientConstants
                && parameters.featureFlags == FeatureFlags
                && parameters.storageRoot == StorageRoot
                && parameters.workDir == WorkDir
                && parameters.embeddedPluginPaths == JarList
                && parameters.connectedModeEmbeddedPluginPathsByKey.Count == 0
                && parameters.enabledLanguagesInStandaloneMode.SequenceEqual(new[]
                {
                    Language.C,
                    Language.CPP,
                    Language.CS,
                    Language.VBNET,
                    Language.JS,
                    Language.TS,
                    Language.CSS,
                    Language.SECRETS
                })
                && parameters.extraEnabledLanguagesInConnectedMode.Count == 0
                && parameters.sonarQubeConnections.SequenceEqual(new[] { SonarQubeConnection1, SonarQubeConnection2 })
                && parameters.sonarCloudConnections.SequenceEqual(new[] { SonarCloudConnection })
                && parameters.sonarlintUserHome == UserHome
                && parameters.standaloneRuleConfigByKey.Count == 0
                && !parameters.isFocusOnNewCode
                && parameters.telemetryConstantAttributes == TelemetryConstants
                && parameters.clientNodeJsPath == null));
            telemetryService.DisableTelemetry();
            configScopeUpdater.UpdateConfigScopeForCurrentSolution(Binding);
        });
    }

    private void SetUpSuccessfulInitialization(ISLCoreRpcFactory slCoreRpcFactory,
        ISLCoreConstantsProvider constantsProvider,
        ISLCoreFoldersProvider foldersProvider,
        IServerConnectionsProvider connectionsProvider,
        ISLCoreEmbeddedPluginJarLocator jarLocator,
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        out ILifecycleManagementSLCoreService lifecycleManagement,
        out ITelemetrySLCoreService telemetry)
    {
        SetUpSLCoreRpcFactory(slCoreRpcFactory, out var rpc);
        SetUpSLCoreRpc(rpc, out var serviceProvider);
        SetUpSLCoreServiceProvider(serviceProvider, out lifecycleManagement, out telemetry);
        constantsProvider.ClientConstants.Returns(ClientConstants);
        constantsProvider.FeatureFlags.Returns(FeatureFlags);
        constantsProvider.TelemetryConstants.Returns(TelemetryConstants);
        foldersProvider.GetWorkFolders().Returns(new SLCoreFolders(StorageRoot, WorkDir, UserHome));
        connectionsProvider.GetServerConnections().Returns(new Dictionary<string, ServerConnectionConfiguration>
        {
            { SonarQubeConnection1.connectionId, SonarQubeConnection1 },
            { SonarQubeConnection2.connectionId, SonarQubeConnection2 },
            { SonarCloudConnection.connectionId, SonarCloudConnection }
        });
        jarLocator.ListJarFiles().Returns(JarList);
        activeSolutionBoundTracker.CurrentConfiguration.Returns(new BindingConfiguration(Binding, SonarLintMode.Connected, "dir"));
    }

    #region RpcSetUp

    private void SetUpSLCoreRpcFactory(ISLCoreRpcFactory slCoreRpcFactory, out ISLCoreRpc slCoreRpc)
    {
        slCoreRpc = Substitute.For<ISLCoreRpc>();
        slCoreRpcFactory.StartNewRpcInstance().Returns(slCoreRpc);
    }

    private void SetUpSLCoreRpc(ISLCoreRpc slCoreRpc, out ISLCoreServiceProvider slCoreServiceProvider)
    {
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        slCoreRpc.ServiceProvider.Returns(slCoreServiceProvider);
    }

    private void SetUpSLCoreServiceProvider(ISLCoreServiceProvider slCoreServiceProvider,
        out ILifecycleManagementSLCoreService lifecycleManagementSlCoreService, out ITelemetrySLCoreService telemetrySlCoreService)
    {
        var managementService = Substitute.For<ILifecycleManagementSLCoreService>();
        lifecycleManagementSlCoreService = managementService;
        var telemetryService = Substitute.For<ITelemetrySLCoreService>();
        telemetrySlCoreService = telemetryService;
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<ILifecycleManagementSLCoreService>()).Returns(x =>
        {
            x[0] = managementService;
            return true;
        });
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<ITelemetrySLCoreService>()).Returns(x =>
        {
            x[0] = telemetryService;
            return true;
        });
    }

    #endregion

    private SLCoreHandle CreateTestSubject(out ISLCoreRpcFactory slCoreRpcFactory,
        out ISLCoreConstantsProvider constantsProvider,
        out ISLCoreFoldersProvider slCoreFoldersProvider,
        out IServerConnectionsProvider serverConnectionConfigurationProvider,
        out ISLCoreEmbeddedPluginJarLocator slCoreEmbeddedPluginJarProvider,
        out IActiveSolutionBoundTracker activeSolutionBoundTracker,
        out IConfigScopeUpdater configScopeUpdater,
        out IThreadHandling threadHandling)
    {
        slCoreRpcFactory = Substitute.For<ISLCoreRpcFactory>();
        constantsProvider = Substitute.For<ISLCoreConstantsProvider>();
        slCoreFoldersProvider = Substitute.For<ISLCoreFoldersProvider>();
        serverConnectionConfigurationProvider = Substitute.For<IServerConnectionsProvider>();
        slCoreEmbeddedPluginJarProvider = Substitute.For<ISLCoreEmbeddedPluginJarLocator>();
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        configScopeUpdater = Substitute.For<IConfigScopeUpdater>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();

        return new SLCoreHandle(slCoreRpcFactory,
            constantsProvider,
            slCoreFoldersProvider,
            serverConnectionConfigurationProvider,
            slCoreEmbeddedPluginJarProvider,
            activeSolutionBoundTracker,
            configScopeUpdater,
            threadHandling);
    }

    internal class AnySLCoreService : Arg.AnyType, ISLCoreService
    {
    }
}
