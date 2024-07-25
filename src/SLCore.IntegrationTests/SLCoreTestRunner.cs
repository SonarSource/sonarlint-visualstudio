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

using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.VsInfo;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.SLCore;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.SLCore;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Core.Process;
using SonarLint.VisualStudio.SLCore.NodeJS;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

public sealed class SLCoreTestRunner : IDisposable
{
    private readonly ILogger logger;
    private readonly List<ISLCoreListener> listenersToSetUp = new();
    private string privateFolder;
    private string storageRoot;
    private string workDir;
    private string userHome;
    private readonly ISLCoreProcessFactory slCoreTestProcessFactory;
    private SLCoreInstanceHandle slCoreInstanceHandle;
    internal ISLCoreServiceProvider SLCoreServiceProvider => slCoreInstanceHandle?.SLCoreRpc?.ServiceProvider;
    private readonly string testName;
    private readonly ISLCoreRuleSettings slCoreRulesSettings = Substitute.For<ISLCoreRuleSettings>();

    public SLCoreTestRunner(ILogger logger, ILogger slCoreErrorLogger, string testName)
    {
        this.logger = logger;

        this.testName = testName;

        SetUpLocalFolders();

        slCoreTestProcessFactory = new SLCoreProcessFactory(new SLCoreErrorLoggerFactory(slCoreErrorLogger, new NoOpThreadHandler()));
    }

    public void AddListener(ISLCoreListener listener)
    {
        if (slCoreInstanceHandle is not null)
        {
            throw new InvalidOperationException("Listening already started");
        }
        listenersToSetUp.Add(listener);
    }

    public void MockInitialSlCoreRulesSettings(Dictionary<string, StandaloneRuleConfigDto> rulesSettings)
    {
        slCoreRulesSettings.RulesSettings.Returns(rulesSettings);
    }

    public void Start()
    {
        try
        {
            Environment.SetEnvironmentVariable("SONARLINT_LOG_RPC", "true", EnvironmentVariableTarget.Process);

            var rootLocator = Substitute.For<IVsixRootLocator>();
            rootLocator.GetVsixRoot().Returns(DependencyLocator.SloopBasePath);
            var slCoreLocator = new SLCoreLocator(rootLocator, string.Empty);

            var constantsProvider = Substitute.For<ISLCoreConstantsProvider>();
            constantsProvider.ClientConstants.Returns(new ClientConstantsDto("SLVS_Integration_Tests",
                $"SLVS_Integration_Tests/{VersionHelper.SonarLintVersion}", Process.GetCurrentProcess().Id));
            constantsProvider.FeatureFlags.Returns(new FeatureFlagsDto(true, true, false, true, false, false, true, false));
            constantsProvider.TelemetryConstants.Returns(new TelemetryClientConstantAttributesDto("slvs_integration_tests", "SLVS Integration Tests",
                VersionHelper.SonarLintVersion, "17.0", new()));
            SetLanguagesConfigurationToDefaults(constantsProvider);

            var foldersProvider = Substitute.For<ISLCoreFoldersProvider>();
            foldersProvider.GetWorkFolders().Returns(new SLCoreFolders(storageRoot, workDir, userHome));

            var connectionProvider = Substitute.For<IServerConnectionsProvider>();
            connectionProvider.GetServerConnections().Returns(new Dictionary<string, ServerConnectionConfiguration>());

            var jarProvider = Substitute.For<ISLCoreEmbeddedPluginJarLocator>();
            jarProvider.ListJarFiles().Returns(DependencyLocator.AnalyzerPlugins);
            
            var compatibleNodeLocator = Substitute.For<INodeLocationProvider>();
            compatibleNodeLocator.Get().Returns((string)null);

            var noOpActiveSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
            noOpActiveSolutionBoundTracker.CurrentConfiguration.Returns(BindingConfiguration.Standalone);
            var noOpConfigScopeUpdater = Substitute.For<IConfigScopeUpdater>();

            slCoreInstanceHandle = new SLCoreInstanceHandle(new SLCoreRpcFactory(slCoreTestProcessFactory, slCoreLocator,
                    new SLCoreJsonRpcFactory(new RpcMethodNameTransformer()),
                    new RpcDebugger(new FileSystem(), Path.Combine(privateFolder, "logrpc.log")),
                    new SLCoreServiceProvider(new NoOpThreadHandler(), logger),
                    new SLCoreListenerSetUp(listenersToSetUp)),
                constantsProvider,
                foldersProvider,
                connectionProvider,
                jarProvider,
                compatibleNodeLocator,
                noOpActiveSolutionBoundTracker,
                noOpConfigScopeUpdater,
                new NoOpThreadHandler(),
                slCoreRulesSettings);
            slCoreInstanceHandle.Initialize();
        }
        finally
        {
            Environment.SetEnvironmentVariable("SONARLINT_LOG_RPC", null, EnvironmentVariableTarget.Process);
        }
    }

    private static void SetLanguagesConfigurationToDefaults(ISLCoreConstantsProvider constantsProvider)
    {
        var defaultConstantsProvider = new SLCoreConstantsProvider(Substitute.For<IVsInfoProvider>());
        constantsProvider.LanguagesInStandaloneMode.Returns(defaultConstantsProvider.LanguagesInStandaloneMode);
        constantsProvider.SLCoreAnalyzableLanguages.Returns(defaultConstantsProvider.SLCoreAnalyzableLanguages);
    }

    public void Dispose()
    {
        slCoreInstanceHandle?.Dispose();
    }

    private void SetUpLocalFolders()
    {
        privateFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "slcore", testName);
        storageRoot = Path.Combine(privateFolder, "storageRoot");
        workDir = Path.Combine(privateFolder, "workDir");
        userHome = Path.Combine(privateFolder, "userHome");

        if (Directory.Exists(privateFolder))
        {
            DeleteDirectoryForLongPaths(privateFolder);
        }

        Directory.CreateDirectory(privateFolder);
        Directory.CreateDirectory(storageRoot);
        Directory.CreateDirectory(workDir);
        Directory.CreateDirectory(userHome);
    }

    private static void DeleteDirectoryForLongPaths(string dirToDelete)
    {
        Directory.Delete(@"\\?\" + dirToDelete, true);
    }
}
