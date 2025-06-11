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

using NSubstitute.ClearExtensions;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.EsLintBridge;
using SonarLint.VisualStudio.SLCore.NodeJS;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.State;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.SLCore.UnitTests;

[TestClass]
public class SLCoreInstanceHandleTests
{
    private const string StorageRoot = "storageRootSl";
    private const string WorkDir = "workDirSl";
    private const string UserHome = "userHomeSl";

    private static readonly ClientConstantInfoDto ClientConstantInfo = new(default, default);
    private static readonly HashSet<BackendCapability> BackendCapabilities = [BackendCapability.PROJECT_SYNCHRONIZATION];
    private static readonly TelemetryClientConstantAttributesDto TelemetryConstants = new(default, default, default, default, default);

    private static readonly SonarQubeConnectionConfigurationDto SonarQubeConnection1 = new("sq1", true, "http://localhost/");
    private static readonly SonarQubeConnectionConfigurationDto SonarQubeConnection2 = new("sq2", true, "https://next.sonarqube.org/");
    private static readonly SonarCloudConnectionConfigurationDto SonarCloudConnection = new("sc", true, "https://sonarcloud.io/");

    private static readonly BoundServerProject Binding = new("solution", "projectKey", new ServerConnection.SonarQube(new Uri("http://localhost")));

    private static readonly List<string> JarList = new() { "jar1" };
    private static readonly Dictionary<string, string> ConnectedModeJarList = new() { { "key", "jar1" } };
    private ISLCoreRpcFactory slCoreRpcFactory;
    private ISLCoreServiceProvider serviceProvider;
    private ISLCoreConstantsProvider constantsProvider;
    private ISLCoreLanguageProvider slCoreLanguageProvider;
    private ISLCoreFoldersProvider foldersProvider;
    private IServerConnectionsProvider connectionsProvider;
    private ISLCoreEmbeddedPluginJarLocator jarLocator;
    private INodeLocationProvider nodeLocator;
    private IEsLintBridgeLocator esLintBridgeLocator;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private IConfigScopeUpdater configScopeUpdater;
    private IThreadHandling threadHandling;
    private ISLCoreRuleSettingsProvider slCoreRuleSettingsProvider;
    private SLCoreInstanceHandle testSubject;
    private ISlCoreTelemetryMigrationProvider telemetryMigrationProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        slCoreRpcFactory = Substitute.For<ISLCoreRpcFactory>();
        serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        constantsProvider = Substitute.For<ISLCoreConstantsProvider>();
        slCoreLanguageProvider = Substitute.For<ISLCoreLanguageProvider>();
        foldersProvider = Substitute.For<ISLCoreFoldersProvider>();
        connectionsProvider = Substitute.For<IServerConnectionsProvider>();
        jarLocator = Substitute.For<ISLCoreEmbeddedPluginJarLocator>();
        nodeLocator = Substitute.For<INodeLocationProvider>();
        esLintBridgeLocator = Substitute.For<IEsLintBridgeLocator>();
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        configScopeUpdater = Substitute.For<IConfigScopeUpdater>();
        threadHandling = Substitute.For<IThreadHandling>();
        slCoreRuleSettingsProvider = Substitute.For<ISLCoreRuleSettingsProvider>();
        telemetryMigrationProvider = Substitute.For<ISlCoreTelemetryMigrationProvider>();

        testSubject = new SLCoreInstanceHandle(
            slCoreRpcFactory,
            serviceProvider,
            constantsProvider,
            slCoreLanguageProvider,
            foldersProvider,
            connectionsProvider,
            jarLocator,
            nodeLocator,
            esLintBridgeLocator,
            activeSolutionBoundTracker,
            configScopeUpdater,
            slCoreRuleSettingsProvider,
            telemetryMigrationProvider,
            threadHandling);
    }

    [TestMethod]
    public void Initialize_ThrowsIfServicesUnavailable()
    {
        SetUpSLCoreRpcFactory(slCoreRpcFactory, out _);
        serviceProvider.TryGetTransientService(out Arg.Any<AnySLCoreService>()).ReturnsForAnyArgs(false);

        var act = () => testSubject.Initialize();

        act.Should().Throw<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [DataTestMethod]
    [DataRow("some/node/path", "vsix/esLintBridge")]
    [DataRow(null, null)]
    public void Initialize_SuccessfullyInitializesInCorrectOrder(string nodeJsPath, string esLintBridgePath)
    {
        SetUpLanguages(slCoreLanguageProvider, [], [], []);
        SetUpSuccessfulInitialization(out var lifecycleManagement, out _);
        nodeLocator.Get().Returns(nodeJsPath);
        esLintBridgeLocator.Get().Returns(esLintBridgePath);
        var telemetryMigrationDto = new TelemetryMigrationDto(default, default, default);
        telemetryMigrationProvider.Get().Returns(telemetryMigrationDto);

        testSubject.Initialize();

        Received.InOrder(() =>
        {
            threadHandling.ThrowIfOnUIThread();
            slCoreRpcFactory.StartNewRpcInstance();
            lifecycleManagement.Initialize(Arg.Is<InitializeParams>(parameters =>
                parameters.clientConstantInfo == ClientConstantInfo
                && parameters.backendCapabilities == BackendCapabilities
                && parameters.storageRoot == StorageRoot
                && parameters.workDir == WorkDir
                && parameters.embeddedPluginPaths == JarList
                && parameters.connectedModeEmbeddedPluginPathsByKey.Count == ConnectedModeJarList.Count
                && parameters.sonarQubeConnections.SequenceEqual(new[] { SonarQubeConnection1, SonarQubeConnection2 })
                && parameters.sonarCloudConnections.SequenceEqual(new[] { SonarCloudConnection })
                && parameters.sonarlintUserHome == UserHome
                && parameters.standaloneRuleConfigByKey.Count == 0
                && !parameters.isFocusOnNewCode
                && parameters.telemetryConstantAttributes == TelemetryConstants
                && parameters.languageSpecificRequirements.jsTsRequirements.clientNodeJsPath == nodeJsPath
                && parameters.languageSpecificRequirements.jsTsRequirements.bundlePath == esLintBridgePath
                && parameters.telemetryMigration == telemetryMigrationDto
                && parameters.automaticAnalysisEnabled));
            activeSolutionBoundTracker.InitializationProcessor.InitializeAsync();
            configScopeUpdater.UpdateConfigScopeForCurrentSolution(Binding);
        });
    }

    [TestMethod]
    public void Initialize_UsesProvidedLanguageConfiguration()
    {
        List<Language> standalone = [Language.CS, Language.HTML];
        List<Language> connected = [Language.VBNET, Language.TSQL];
        List<Language> disabledAnalysis = [Language.CPP, Language.JS];
        SetUpLanguages(slCoreLanguageProvider, standalone, connected, disabledAnalysis);
        SetUpSuccessfulInitialization(out var lifecycleManagement, out _);

        testSubject.Initialize();

        var initializeParams = (InitializeParams)lifecycleManagement.ReceivedCalls().Single().GetArguments().Single()!;
        initializeParams.enabledLanguagesInStandaloneMode.Should().BeSameAs(standalone);
        initializeParams.extraEnabledLanguagesInConnectedMode.Should().BeSameAs(connected);
        initializeParams.disabledPluginKeysForAnalysis.Should().BeEquivalentTo(disabledAnalysis.Select(l => l.GetPluginKey()));
    }

    [TestMethod]
    public void Initialize_ProvidesRulesSettings()
    {
        SetUpSuccessfulInitialization(out var lifecycleManagement, out _);
        slCoreRuleSettingsProvider.GetSLCoreRuleSettings().Returns(new Dictionary<string, StandaloneRuleConfigDto>() { { "rule1", new StandaloneRuleConfigDto(true, []) } });

        testSubject.Initialize();

        lifecycleManagement.Received(1).Initialize(Arg.Is<InitializeParams>(param => param.standaloneRuleConfigByKey.SequenceEqual(slCoreRuleSettingsProvider.GetSLCoreRuleSettings())));
    }

    [TestMethod]
    public void Dispose_Initialized_ShutsDownAndDisposesRpc()
    {
        SetUpThreadHandling(threadHandling);
        SetUpLanguages(slCoreLanguageProvider, [], [], []);

        SetUpSuccessfulInitialization(out var lifecycleManagement, out var rpc);
        testSubject.Initialize();

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
        SetUpThreadHandling(threadHandling);
        SetUpLanguages(slCoreLanguageProvider, [], [], []);

        SetUpSuccessfulInitialization(out var lifecycleManagement, out var rpc);
        lifecycleManagement.When(x => x.Shutdown()).Do(_ => throw new Exception());
        testSubject.Initialize();
        serviceProvider.ClearSubstitute();
        serviceProvider.ClearReceivedCalls();
        serviceProvider.TryGetTransientService(out Arg.Any<AnySLCoreService>()).Throws(new Exception());

        var act = () => testSubject.Dispose();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Dispose_IgnoresShutdownException()
    {
        SetUpThreadHandling(threadHandling);
        SetUpLanguages(slCoreLanguageProvider, [], [], []);

        SetUpSuccessfulInitialization(out var lifecycleManagement, out var rpc);
        lifecycleManagement.When(x => x.Shutdown()).Do(_ => throw new Exception());
        testSubject.Initialize();

        serviceProvider.ClearReceivedCalls();
        var act = () => testSubject.Dispose();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Dispose_ConnectionDied_DisposesRpc()
    {
        SetUpThreadHandling(threadHandling);
        SetUpLanguages(slCoreLanguageProvider, [], [], []);

        SetUpSuccessfulInitialization(out var lifecycleManagement, out var rpc);
        testSubject.Initialize();

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
        var act = () => testSubject.Dispose();

        act.Should().NotThrow();
    }

    private void SetUpSuccessfulInitialization(out ILifecycleManagementSLCoreService lifecycleManagement, out ISLCoreRpc rpc)
    {
        SetUpSLCoreRpcFactory(slCoreRpcFactory, out rpc);
        SetUpSLCoreServiceProvider(serviceProvider, out lifecycleManagement);
        constantsProvider.ClientConstants.Returns(ClientConstantInfo);
        constantsProvider.BackendCapabilities.Returns(BackendCapabilities);
        constantsProvider.TelemetryConstants.Returns(TelemetryConstants);

        foldersProvider.GetWorkFolders().Returns(new SLCoreFolders(StorageRoot, WorkDir, UserHome));
        connectionsProvider.GetServerConnections().Returns(new Dictionary<string, ServerConnectionConfigurationDtoBase>
        {
            { SonarQubeConnection1.connectionId, SonarQubeConnection1 }, { SonarQubeConnection2.connectionId, SonarQubeConnection2 }, { SonarCloudConnection.connectionId, SonarCloudConnection }
        });
        jarLocator.ListJarFiles().Returns(JarList);
        jarLocator.ListConnectedModeEmbeddedPluginPathsByKey().Returns(ConnectedModeJarList);
        activeSolutionBoundTracker.CurrentConfiguration.Returns(new BindingConfiguration(Binding, SonarLintMode.Connected, "dir"));
        slCoreRuleSettingsProvider.GetSLCoreRuleSettings().Returns(new Dictionary<string, StandaloneRuleConfigDto>());
    }

    private void SetUpLanguages(
        ISLCoreLanguageProvider slCoreLanguageProvider,
        List<Language> standalone,
        List<Language> connected,
        List<Language> disabledAnalysis)
    {
        slCoreLanguageProvider.LanguagesInStandaloneMode.Returns(standalone);
        slCoreLanguageProvider.ExtraLanguagesInConnectedMode.Returns(connected);
        slCoreLanguageProvider.LanguagesWithDisabledAnalysis.Returns(disabledAnalysis);
    }

    #region RpcSetUp

    private void SetUpSLCoreRpcFactory(ISLCoreRpcFactory slCoreRpcFactory, out ISLCoreRpc slCoreRpc)
    {
        slCoreRpc = Substitute.For<ISLCoreRpc>();
        slCoreRpcFactory.StartNewRpcInstance().Returns(slCoreRpc);
    }

    private void SetUpSLCoreServiceProvider(
        ISLCoreServiceProvider slCoreServiceProvider,
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

    internal class AnySLCoreService : Arg.AnyType, ISLCoreService
    {
    }
}
