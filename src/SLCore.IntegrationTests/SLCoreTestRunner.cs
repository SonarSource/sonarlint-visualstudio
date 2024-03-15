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
using NSubstitute;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Core.Process;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
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
    private readonly SLCoreTestProcessFactory slCoreTestProcessFactory;
    private SLCoreHandle slCoreHandle;
    internal ISLCoreServiceProvider SLCoreServiceProvider => slCoreHandle?.SLCoreRpc?.ServiceProvider;

    public SLCoreTestRunner(ILogger logger, string testName)
    {
        this.logger = logger;

        SetUpLocalFolders(testName);

        slCoreTestProcessFactory = new SLCoreTestProcessFactory(new SLCoreProcessFactory(),
            Path.Combine(privateFolder, "logstderr.txt"),
            Path.Combine(privateFolder, "logrpc.txt"));
    }

    public void AddListener(ISLCoreListener listener)
    {
        if (slCoreHandle is not null)
        {
            throw new InvalidOperationException("Listening already started");
        }
        listenersToSetUp.Add(listener);
    }

    public async Task Start()
    {
        var slCoreLocator = Substitute.For<ISLCoreLocator>();
        slCoreLocator.LocateExecutable().Returns(new SLCoreLaunchParameters("cmd.exe", $"/c {DependencyLocator.SloopBatPath}"));

        var constantsProvider = Substitute.For<ISLCoreConstantsProvider>();
        constantsProvider.ClientConstants.Returns(new ClientConstantsDto("SLVS_Integration_Tests",
            $"SLVS_Integration_Tests/{VersionHelper.SonarLintVersion}"));
        constantsProvider.FeatureFlags.Returns(new FeatureFlagsDto(true, true, false, true, false, false, true));
        constantsProvider.TelemetryConstants.Returns(new TelemetryClientConstantAttributesDto("slvs_integration_tests", "SLVS Integration Tests",
            VersionHelper.SonarLintVersion, "16.0", new()));

        var foldersProvider = Substitute.For<ISLCoreFoldersProvider>();
        foldersProvider.GetWorkFolders().Returns(new SLCoreFolders(storageRoot, workDir, userHome));

        var connectionProvider = Substitute.For<IServerConnectionsProvider>();
        connectionProvider.GetServerConnections().Returns(new Dictionary<string, ServerConnectionConfiguration>());

        var jarProvider = Substitute.For<ISLCoreEmbeddedPluginJarLocator>();
        jarProvider.ListJarFiles().Returns(DependencyLocator.AnalyzerPlugins);

        var noOpActiveSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        noOpActiveSolutionBoundTracker.CurrentConfiguration.Returns(BindingConfiguration.Standalone);
        var noOpConfigScopeUpdater = Substitute.For<IConfigScopeUpdater>();

        slCoreHandle = new SLCoreHandle(new SLCoreRpcFactory(slCoreTestProcessFactory, slCoreLocator,
                new SLCoreJsonRpcFactory(new RpcMethodNameTransformer()),
                new SLCoreServiceProvider(new NoOpThreadHandler(), logger),
                new SLCoreListenerSetUp(listenersToSetUp)),
            constantsProvider,
            foldersProvider,
            connectionProvider,
            jarProvider,
            noOpActiveSolutionBoundTracker,
            noOpConfigScopeUpdater,
            new NoOpThreadHandler());
        await slCoreHandle.InitializeAsync();
    }

    public void Dispose()
    {
        slCoreHandle.Dispose();
    }

    private void SetUpLocalFolders(string testName)
    {
        privateFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "slcore", testName); // add unique identifier to prevent override between tests?
        storageRoot = Path.Combine(privateFolder, "storageRoot");
        workDir = Path.Combine(privateFolder, "workDir");
        userHome = Path.Combine(privateFolder, "userHome");

        if (Directory.Exists(privateFolder))
        {
            Directory.Delete(privateFolder, true);
        }

        Directory.CreateDirectory(privateFolder);
        Directory.CreateDirectory(storageRoot);
        Directory.CreateDirectory(workDir);
        Directory.CreateDirectory(userHome);
    }
}
