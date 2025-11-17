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

using NSubstitute.ClearExtensions;
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
    private static readonly List<string> DisabledAnalysisPluginKeys = [Language.CS.GetPluginKey()];
    private ISLCoreRpcFactory slCoreRpcFactory;
    private ISLCoreRpcManager rpcManager;
    private ISLCoreConstantsProvider constantsProvider;
    private ISLCoreLanguageProvider slCoreLanguageProvider;
    private ISLCoreFoldersProvider foldersProvider;
    private IServerConnectionsProvider connectionsProvider;
    private ISLCoreEmbeddedPluginProvider jarProvider;
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
        rpcManager = Substitute.For<ISLCoreRpcManager>();
        constantsProvider = Substitute.For<ISLCoreConstantsProvider>();
        slCoreLanguageProvider = Substitute.For<ISLCoreLanguageProvider>();
        foldersProvider = Substitute.For<ISLCoreFoldersProvider>();
        connectionsProvider = Substitute.For<IServerConnectionsProvider>();
        jarProvider = Substitute.For<ISLCoreEmbeddedPluginProvider>();
        nodeLocator = Substitute.For<INodeLocationProvider>();
        esLintBridgeLocator = Substitute.For<IEsLintBridgeLocator>();
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        configScopeUpdater = Substitute.For<IConfigScopeUpdater>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        slCoreRuleSettingsProvider = Substitute.For<ISLCoreRuleSettingsProvider>();
        telemetryMigrationProvider = Substitute.For<ISlCoreTelemetryMigrationProvider>();

        testSubject = new SLCoreInstanceHandle(
            slCoreRpcFactory,
            rpcManager,
            constantsProvider,
            slCoreLanguageProvider,
            foldersProvider,
            connectionsProvider,
            jarProvider,
            nodeLocator,
            esLintBridgeLocator,
            activeSolutionBoundTracker,
            configScopeUpdater,
            slCoreRuleSettingsProvider,
            telemetryMigrationProvider,
            threadHandling);
    }

    [TestMethod]
    public void Initialize_RpcManagerThrows_DoesNotCatch()
    {
        SetUpFullConfiguration(out _);
        const string exceptionMessage = "test exception";
        var exception = new Exception(exceptionMessage);
        rpcManager.When(x => x.Initialize(Arg.Any<InitializeParams>())).Throw(exception);

        var act = () => testSubject.Initialize();

        act.Should().ThrowExactly<Exception>().WithMessage(exceptionMessage);
    }

    [DataTestMethod]
    [DataRow("some/node/path", "vsix/esLintBridge")]
    [DataRow(null, null)]
    public void Initialize_SuccessfullyInitializesInCorrectOrder(string nodeJsPath, string esLintBridgePath)
    {
        SetUpLanguages([], []);
        SetUpFullConfiguration(out _);
        nodeLocator.Get().Returns(nodeJsPath);
        esLintBridgeLocator.Get().Returns(esLintBridgePath);
        var telemetryMigrationDto = new TelemetryMigrationDto(default, default, default);
        telemetryMigrationProvider.Get().Returns(telemetryMigrationDto);

        testSubject.Initialize();

        Received.InOrder(() =>
        {
            threadHandling.ThrowIfOnUIThread();
            slCoreRpcFactory.StartNewRpcInstance();
            rpcManager.Initialize(Arg.Is<InitializeParams>(parameters =>
                parameters.clientConstantInfo == ClientConstantInfo
                && parameters.backendCapabilities == BackendCapabilities
                && parameters.storageRoot == StorageRoot
                && parameters.workDir == WorkDir
                && parameters.embeddedPluginPaths == JarList
                && parameters.connectedModeEmbeddedPluginPathsByKey.Count == ConnectedModeJarList.Count
                && parameters.disabledPluginKeysForAnalysis.SequenceEqual(DisabledAnalysisPluginKeys)
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
        SetUpLanguages(standalone, connected);
        SetUpFullConfiguration(out _);

        testSubject.Initialize();

        var initializeParams = (InitializeParams)rpcManager.ReceivedCalls().Single().GetArguments().Single()!;
        initializeParams.enabledLanguagesInStandaloneMode.Should().BeSameAs(standalone);
        initializeParams.extraEnabledLanguagesInConnectedMode.Should().BeSameAs(connected);
    }

    [TestMethod]
    public void Initialize_ProvidesRulesSettings()
    {
        SetUpFullConfiguration(out _);
        slCoreRuleSettingsProvider.GetSLCoreRuleSettings().Returns(new Dictionary<string, StandaloneRuleConfigDto>() { { "rule1", new StandaloneRuleConfigDto(true, []) } });

        testSubject.Initialize();

        rpcManager.Received(1).Initialize(Arg.Is<InitializeParams>(param => param.standaloneRuleConfigByKey.SequenceEqual(slCoreRuleSettingsProvider.GetSLCoreRuleSettings())));
    }

    [TestMethod]
    public void Dispose_Initialized_ShutsDownAndDisposesRpc()
    {
        SetUpLanguages([], []);

        SetUpFullConfiguration(out var rpc);
        testSubject.Initialize();

        rpcManager.ClearReceivedCalls();
        testSubject.Dispose();

        Received.InOrder(() =>
        {
            threadHandling.Run(Arg.Any<Func<Task<int>>>());
            threadHandling.SwitchToBackgroundThread();
            rpcManager.Shutdown();
        });
        rpc.Received().Dispose();
    }

    [TestMethod]
    public void Dispose_IgnoresShutdownException()
    {
        SetUpLanguages([], []);

        SetUpFullConfiguration(out var rpc);
        rpcManager.When(x => x.Shutdown()).Do(_ => throw new Exception());
        testSubject.Initialize();

        rpcManager.ClearReceivedCalls();
        var act = () => testSubject.Dispose();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Dispose_ConnectionDied_DisposesRpc()
    {
        SetUpLanguages([], []);

        SetUpFullConfiguration(out var rpc);
        testSubject.Initialize();

        rpcManager.ClearSubstitute();
        rpcManager.ClearReceivedCalls();
        testSubject.Dispose();

        rpc.Received().Dispose();
        Received.InOrder(() =>
        {
            threadHandling.Run(Arg.Any<Func<Task<int>>>());
            threadHandling.SwitchToBackgroundThread();
            rpcManager.Shutdown();
        });
    }

    [TestMethod]
    public void Dispose_NotInitialized_DoesNothing()
    {
        var act = () => testSubject.Dispose();

        act.Should().NotThrow();
    }

    private void SetUpFullConfiguration(out ISLCoreRpc rpc)
    {
        SetUpSLCoreRpcFactory(out rpc);
        constantsProvider.ClientConstants.Returns(ClientConstantInfo);
        constantsProvider.BackendCapabilities.Returns(BackendCapabilities);
        constantsProvider.TelemetryConstants.Returns(TelemetryConstants);

        foldersProvider.GetWorkFolders().Returns(new SLCoreFolders(StorageRoot, WorkDir, UserHome));
        connectionsProvider.GetServerConnections().Returns(new Dictionary<string, ServerConnectionConfigurationDtoBase>
        {
            { SonarQubeConnection1.connectionId, SonarQubeConnection1 }, { SonarQubeConnection2.connectionId, SonarQubeConnection2 }, { SonarCloudConnection.connectionId, SonarCloudConnection }
        });
        jarProvider.ListJarFiles().Returns(JarList);
        jarProvider.ListConnectedModeEmbeddedPluginPathsByKey().Returns(ConnectedModeJarList);
        jarProvider.ListDisabledPluginKeysForAnalysis().Returns(DisabledAnalysisPluginKeys);
        activeSolutionBoundTracker.CurrentConfiguration.Returns(new BindingConfiguration(Binding, SonarLintMode.Connected, "dir"));
        slCoreRuleSettingsProvider.GetSLCoreRuleSettings().Returns(new Dictionary<string, StandaloneRuleConfigDto>());
    }

    private void SetUpLanguages(
        List<Language> standalone,
        List<Language> connected)
    {
        slCoreLanguageProvider.LanguagesInStandaloneMode.Returns(standalone);
        slCoreLanguageProvider.ExtraLanguagesInConnectedMode.Returns(connected);
    }

    #region RpcSetUp

    private void SetUpSLCoreRpcFactory(out ISLCoreRpc slCoreRpc)
    {
        slCoreRpc = Substitute.For<ISLCoreRpc>();
        slCoreRpcFactory.StartNewRpcInstance().Returns(slCoreRpc);
    }

    #endregion

    internal class AnySLCoreService : Arg.AnyType, ISLCoreService
    {
    }
}
