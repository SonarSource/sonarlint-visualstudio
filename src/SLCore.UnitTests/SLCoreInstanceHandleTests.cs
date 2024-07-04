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

using NSubstitute.ClearExtensions;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.SLCore.State;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.SLCore.UnitTests;

[TestClass]
public class SLCoreInstanceHandleTests
{
    private const string StorageRoot = "storageRootSl";
    private const string WorkDir = "workDirSl";
    private const string UserHome = "userHomeSl";
    
    private static readonly ClientConstantsDto ClientConstants = new(default, default, default);
    private static readonly FeatureFlagsDto FeatureFlags = new(default, default, default, default, default, default, default, default);
    private static readonly TelemetryClientConstantAttributesDto TelemetryConstants = new(default, default, default, default, default);

    private static readonly SonarQubeConnectionConfigurationDto SonarQubeConnection1 = new("sq1", true, "http://localhost/");
    private static readonly SonarQubeConnectionConfigurationDto SonarQubeConnection2 = new("sq2", true, "https://next.sonarqube.org/");
    private static readonly SonarCloudConnectionConfigurationDto SonarCloudConnection = new("sc", true, "https://sonarcloud.io/");

    private static readonly BoundSonarQubeProject Binding = new();
    
    private static readonly List<string> JarList = new() { "jar1" };

    [TestMethod]
    public void Initialize_ThrowsIfServicesUnavailable()
    {
        var testSubject = CreateTestSubject(out var slCoreRpcFactory, out _, out _, out _, out _, out _, out _, out _);
        SetUpSLCoreRpcFactory(slCoreRpcFactory, out var rpc);
        SetUpSLCoreRpc(rpc, out var serviceProvider);
        serviceProvider.TryGetTransientService(out Arg.Any<AnySLCoreService>()).ReturnsForAnyArgs(false);

        var act = () => testSubject.Initialize();

        act.Should().Throw<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void Initialize_SuccessfullyInitializesInCorrectOrder()
    {
        var testSubject = CreateTestSubject(out var slCoreRpcFactory,
            out var constantsProvider,
            out var foldersProvider,
            out var connectionsProvider,
            out var jarLocator,
            out var activeSolutionBoundTracker,
            out var configScopeUpdater,
            out var threadHandling);
        SetUpLanguages(constantsProvider, [], []);

        SetUpSuccessfulInitialization(slCoreRpcFactory,
            constantsProvider,
            foldersProvider,
            connectionsProvider,
            jarLocator,
            activeSolutionBoundTracker,
            out var lifecycleManagement,
            out _);

        testSubject.Initialize();

        Received.InOrder(() =>
        {
            threadHandling.ThrowIfOnUIThread();
            slCoreRpcFactory.StartNewRpcInstance();
            lifecycleManagement.Initialize(Arg.Is<InitializeParams>(parameters =>
                parameters.clientConstantInfo == ClientConstants
                && parameters.featureFlags == FeatureFlags
                && parameters.storageRoot == StorageRoot
                && parameters.workDir == WorkDir
                && parameters.embeddedPluginPaths == JarList
                && parameters.connectedModeEmbeddedPluginPathsByKey.Count == 0
                && parameters.sonarQubeConnections.SequenceEqual(new[] { SonarQubeConnection1, SonarQubeConnection2 })
                && parameters.sonarCloudConnections.SequenceEqual(new[] { SonarCloudConnection })
                && parameters.sonarlintUserHome == UserHome
                && parameters.standaloneRuleConfigByKey.Count == 0
                && !parameters.isFocusOnNewCode
                && parameters.telemetryConstantAttributes == TelemetryConstants
                && parameters.languageSpecificRequirements.clientNodeJsPath == null));
            configScopeUpdater.UpdateConfigScopeForCurrentSolution(Binding);
        });
    }
    
    [TestMethod]
    public void Initialize_NoLanguagesAnalysisEnabled_DisablesAllLanguages()
    {
        var testSubject = CreateTestSubject(out var slCoreRpcFactory,
            out var constantsProvider,
            out var foldersProvider,
            out var connectionsProvider,
            out var jarLocator,
            out var activeSolutionBoundTracker,
            out _,
            out _);
        SetUpLanguages(constantsProvider, [Language.ABAP, Language.APEX, Language.YAML, Language.XML], []);

        SetUpSuccessfulInitialization(slCoreRpcFactory,
            constantsProvider,
            foldersProvider,
            connectionsProvider,
            jarLocator,
            activeSolutionBoundTracker,
            out var lifecycleManagement,
            out _);

        testSubject.Initialize();

        var initializeParams = (InitializeParams)lifecycleManagement.ReceivedCalls().Single().GetArguments().Single()!;
        initializeParams.enabledLanguagesInStandaloneMode.Should().BeEquivalentTo([Language.ABAP, Language.APEX, Language.YAML, Language.XML]);
        initializeParams.extraEnabledLanguagesInConnectedMode.Should().BeEquivalentTo([]);
        Language[] expectedDisabledLanguages = [Language.ABAP, Language.APEX, Language.YAML, Language.XML];
        initializeParams.disabledPluginKeysForAnalysis.Should().BeEquivalentTo(expectedDisabledLanguages.Select(l => l.GetPluginKey()));
    }
    
    [TestMethod]
    public void Initialize_AnalysisPartiallyEnabled_DisablesAllNotEnabledLanguages()
    {
        var testSubject = CreateTestSubject(out var slCoreRpcFactory,
            out var constantsProvider,
            out var foldersProvider,
            out var connectionsProvider,
            out var jarLocator,
            out var activeSolutionBoundTracker,
            out _,
            out _);
        SetUpLanguages(constantsProvider, [Language.ABAP, Language.APEX, Language.YAML, Language.XML], [Language.APEX, Language.YAML]);

        SetUpSuccessfulInitialization(slCoreRpcFactory,
            constantsProvider,
            foldersProvider,
            connectionsProvider,
            jarLocator,
            activeSolutionBoundTracker,
            out var lifecycleManagement,
            out _);

        testSubject.Initialize();

        var initializeParams = (InitializeParams)lifecycleManagement.ReceivedCalls().Single().GetArguments().Single()!;
        initializeParams.enabledLanguagesInStandaloneMode.Should().BeEquivalentTo([Language.ABAP, Language.APEX, Language.YAML, Language.XML]);
        initializeParams.extraEnabledLanguagesInConnectedMode.Should().BeEquivalentTo([]);
        Language[] expectedDisabledLanguages = [Language.ABAP, Language.XML];
        initializeParams.disabledPluginKeysForAnalysis.Should().BeEquivalentTo(expectedDisabledLanguages.Select(l => l.GetPluginKey()));
    }
    
    [TestMethod]
    public void Initialize_AnalysisFullyEnabled_DisablesNoLanguages()
    {
        var testSubject = CreateTestSubject(out var slCoreRpcFactory,
            out var constantsProvider,
            out var foldersProvider,
            out var connectionsProvider,
            out var jarLocator,
            out var activeSolutionBoundTracker,
            out _,
            out _);
        SetUpLanguages(constantsProvider, [Language.ABAP, Language.APEX, Language.YAML, Language.XML], [Language.XML, Language.APEX, Language.YAML, Language.ABAP, ]);

        SetUpSuccessfulInitialization(slCoreRpcFactory,
            constantsProvider,
            foldersProvider,
            connectionsProvider,
            jarLocator,
            activeSolutionBoundTracker,
            out var lifecycleManagement,
            out _);

        testSubject.Initialize();

        var initializeParams = (InitializeParams)lifecycleManagement.ReceivedCalls().Single().GetArguments().Single();
        initializeParams.enabledLanguagesInStandaloneMode.Should().BeEquivalentTo([Language.ABAP, Language.APEX, Language.YAML, Language.XML]);
        initializeParams.extraEnabledLanguagesInConnectedMode.Should().BeEquivalentTo([]);
        initializeParams.disabledPluginKeysForAnalysis.Should().BeEquivalentTo([]);
    }

    [TestMethod]
    public void Dispose_Initialized_ShutsDownAndDisposesRpc()
    {
        var testSubject = CreateTestSubject(out var slCoreRpcFactory,
            out var constantsProvider,
            out var foldersProvider,
            out var connectionsProvider,
            out var jarLocator,
            out var activeSolutionBoundTracker,
            out _,
            out var threadHandling);
        SetUpThreadHandling(threadHandling);
        SetUpLanguages(constantsProvider, [], []);

        SetUpSuccessfulInitialization(slCoreRpcFactory,
            constantsProvider,
            foldersProvider,
            connectionsProvider,
            jarLocator,
            activeSolutionBoundTracker,
            out var lifecycleManagement,
            out var rpc);
        testSubject.Initialize();

        var serviceProvider = rpc.ServiceProvider;
        serviceProvider.ClearReceivedCalls();
        testSubject.Dispose();
        
        
        serviceProvider.Received().TryGetTransientService(out Arg.Any<ILifecycleManagementSLCoreService>());
        Received.InOrder(() =>
        {
            threadHandling.Run(Arg.Any<Func<Task<int>>>());
            threadHandling.SwitchToBackgroundThread();
            serviceProvider.TryGetTransientService(out Arg.Any<ILifecycleManagementSLCoreService>());
            lifecycleManagement.Shutdown();
        });
        rpc.Received().Dispose();
    }
    
    [TestMethod]
    public void Dispose_IgnoresServiceProviderException()
    {
        var testSubject = CreateTestSubject(out var slCoreRpcFactory,
            out var constantsProvider,
            out var foldersProvider,
            out var connectionsProvider,
            out var jarLocator,
            out var activeSolutionBoundTracker,
            out _,
            out var threadHandling);
        SetUpThreadHandling(threadHandling);
        SetUpLanguages(constantsProvider, [], []);

        SetUpSuccessfulInitialization(slCoreRpcFactory,
            constantsProvider,
            foldersProvider,
            connectionsProvider,
            jarLocator,
            activeSolutionBoundTracker,
            out var lifecycleManagement,
            out var rpc);
        lifecycleManagement.When(x => x.Shutdown()).Do(_ => throw new Exception());
        testSubject.Initialize();
        var serviceProvider = rpc.ServiceProvider;
        serviceProvider.ClearSubstitute();
        serviceProvider.ClearReceivedCalls();
        serviceProvider.TryGetTransientService(out Arg.Any<AnySLCoreService>()).Throws(new Exception());
        
        var act = () => testSubject.Dispose();
        
        act.Should().NotThrow();
    }
    
    [TestMethod]
    public void Dispose_IgnoresShutdownException()
    {
        var testSubject = CreateTestSubject(out var slCoreRpcFactory,
            out var constantsProvider,
            out var foldersProvider,
            out var connectionsProvider,
            out var jarLocator,
            out var activeSolutionBoundTracker,
            out _,
            out var threadHandling);
        SetUpThreadHandling(threadHandling);
        SetUpLanguages(constantsProvider, [], []);

        SetUpSuccessfulInitialization(slCoreRpcFactory,
            constantsProvider,
            foldersProvider,
            connectionsProvider,
            jarLocator,
            activeSolutionBoundTracker,
            out var lifecycleManagement,
            out var rpc);
        lifecycleManagement.When(x => x.Shutdown()).Do(_ => throw new Exception());
        testSubject.Initialize();

        var serviceProvider = rpc.ServiceProvider;
        serviceProvider.ClearReceivedCalls();
        var act = () => testSubject.Dispose();
        
        act.Should().NotThrow();
    }
    
    [TestMethod]
    public void Dispose_ConnectionDied_DisposesRpc()
    {
        var testSubject = CreateTestSubject(out var slCoreRpcFactory,
            out var constantsProvider,
            out var foldersProvider,
            out var connectionsProvider,
            out var jarLocator,
            out var activeSolutionBoundTracker,
            out _,
            out var threadHandling);
        SetUpThreadHandling(threadHandling);
        SetUpLanguages(constantsProvider, [], []);

        SetUpSuccessfulInitialization(slCoreRpcFactory,
            constantsProvider,
            foldersProvider,
            connectionsProvider,
            jarLocator,
            activeSolutionBoundTracker,
            out var lifecycleManagement,
            out var rpc);
        testSubject.Initialize();

        var serviceProvider = rpc.ServiceProvider;
        serviceProvider.ClearSubstitute();
        serviceProvider.ClearReceivedCalls();
        serviceProvider.TryGetTransientService(out Arg.Any<AnySLCoreService>()).Returns(false);
        testSubject.Dispose();
        
        serviceProvider.ReceivedWithAnyArgs().TryGetTransientService(out Arg.Any<ILifecycleManagementSLCoreService>());
        rpc.Received().Dispose();
        Received.InOrder(() =>
        {
            threadHandling.Run(Arg.Any<Func<Task<int>>>());
            threadHandling.SwitchToBackgroundThread();
            serviceProvider.TryGetTransientService(out Arg.Any<ILifecycleManagementSLCoreService>());
        });
        lifecycleManagement.DidNotReceive().Shutdown();
    }
    
    [TestMethod]
    public void Dispose_NotInitialized_DoesNothing()
    {
        var testSubject = CreateTestSubject(out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _);
        
        var act = () => testSubject.Dispose();

        act.Should().NotThrow();
    }

    private void SetUpSuccessfulInitialization(ISLCoreRpcFactory slCoreRpcFactory,
        ISLCoreConstantsProvider constantsProvider,
        ISLCoreFoldersProvider foldersProvider,
        IServerConnectionsProvider connectionsProvider,
        ISLCoreEmbeddedPluginJarLocator jarLocator,
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        out ILifecycleManagementSLCoreService lifecycleManagement,
        out ISLCoreRpc rpc)
    {
        SetUpSLCoreRpcFactory(slCoreRpcFactory, out rpc);
        SetUpSLCoreRpc(rpc, out var serviceProvider);
        SetUpSLCoreServiceProvider(serviceProvider, out lifecycleManagement);
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

    private void SetUpLanguages(ISLCoreConstantsProvider constantsProvider,
        List<Language> standalone,
        List<Language> enabledAnalysis)
    {
        constantsProvider.LanguagesInStandaloneMode.Returns(standalone);
        constantsProvider.SLCoreAnalyzableLanguages.Returns(enabledAnalysis);
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
        out ILifecycleManagementSLCoreService lifecycleManagementSlCoreService)
    {
        var managementService = Substitute.For<ILifecycleManagementSLCoreService>();
        lifecycleManagementSlCoreService = managementService;
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<ILifecycleManagementSLCoreService>()).Returns(x =>
        {
            x[0] = managementService;
            return true;
        });
    }

    #endregion

    private void SetUpThreadHandling(IThreadHandling threadHandling)
    {
        threadHandling.Run(Arg.Any<Func<Task<int>>>()).Returns(info => info.Arg<Func<Task<int>>>()().GetAwaiter().GetResult());
        threadHandling.SwitchToBackgroundThread().Returns(new NoOpThreadHandler.NoOpAwaitable());
    }

    private SLCoreInstanceHandle CreateTestSubject(out ISLCoreRpcFactory slCoreRpcFactory,
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
        threadHandling = Substitute.For<IThreadHandling>();

        return new SLCoreInstanceHandle(slCoreRpcFactory,
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
