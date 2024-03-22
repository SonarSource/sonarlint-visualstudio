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
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using SonarLint.VisualStudio.SLCore.State;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.SLCore;

public interface ISLCoreHandler : IDisposable
{
    event EventHandler InstanceDied;
    int CurrentStartNumber { get; }
    void StartInstance();
}

[Export(typeof(ISLCoreHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class SLCoreHandler : ISLCoreHandler
{
    private readonly IAliveConnectionTracker aliveConnectionTracker;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly object lockObject = new object();
    private readonly ISLCoreHandleFactory slCoreHandleFactory;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private ISLCoreHandle currentHandle = null;

    public event EventHandler InstanceDied;
    public int CurrentStartNumber { get; private set; }

    [ImportingConstructor]
    public SLCoreHandler(IAliveConnectionTracker aliveConnectionTracker,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        ISLCoreHandleFactory slCoreHandleFactory,
        IThreadHandling threadHandling,
        ILogger logger)
    {
        this.aliveConnectionTracker = aliveConnectionTracker;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.slCoreHandleFactory = slCoreHandleFactory;
        this.threadHandling = threadHandling;
        this.logger = logger;
    }

    public void StartInstance()
    {
        threadHandling.ThrowIfOnUIThread();

        ISLCoreHandle newHandle;
        
        lock (lockObject)
        {
            if (currentHandle != null)
            {
                throw new InvalidOperationException("Current instance is alive");
            }

            CurrentStartNumber++;
            try
            {
                logger.WriteLine("Creating SLCore instance");
                newHandle = slCoreHandleFactory.CreateInstance();
                currentHandle = newHandle;
            }
            catch (Exception e)
            {
                logger.WriteLine("Error creating SLCore instance");
                logger.LogVerbose(e.ToString());
                InstanceDied?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        threadHandling.RunOnBackgroundThread(async () =>
        {
            await LaunchInstanceAsync(newHandle);
            return 0;
        }).Forget();
    }

    private async Task LaunchInstanceAsync(ISLCoreHandle newHandle)
    {
        try
        {
            logger.WriteLine("Starting SLCore instance");
            await newHandle.InitializeAsync();
            await newHandle.ShutdownTask;
        }
        finally
        {
            HandleInstanceDeath(newHandle);
        }
    }

    private void HandleInstanceDeath(ISLCoreHandle newHandle)
    {
        logger.WriteLine("SLCore instance has died");
        newHandle.Dispose();
        activeConfigScopeTracker.Reset();
        lock (lockObject)
        {
            currentHandle = null;
            InstanceDied?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        currentHandle?.Dispose();
        aliveConnectionTracker?.Dispose();
        activeConfigScopeTracker?.Dispose();
    }
}

public interface ISLCoreHandle : IDisposable
{
    Task InitializeAsync();

    Task ShutdownTask { get; }
}

internal sealed class SLCoreHandle : ISLCoreHandle
{
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly ISLCoreRpcFactory slCoreRpcFactory;
    private readonly IServerConnectionsProvider serverConnectionConfigurationProvider;
    private readonly IConfigScopeUpdater configScopeUpdater;
    private readonly ISLCoreConstantsProvider constantsProvider;
    private readonly ISLCoreFoldersProvider slCoreFoldersProvider;
    private readonly ISLCoreEmbeddedPluginJarLocator slCoreEmbeddedPluginJarProvider;
    private readonly IThreadHandling threadHandling;
    public Task ShutdownTask => SLCoreRpc.ShutdownTask;
    internal ISLCoreRpc SLCoreRpc { get; private set; }


    internal SLCoreHandle(ISLCoreRpcFactory slCoreRpcFactory, ISLCoreConstantsProvider constantsProvider,
        ISLCoreFoldersProvider slCoreFoldersProvider,
        IServerConnectionsProvider serverConnectionConfigurationProvider, ISLCoreEmbeddedPluginJarLocator slCoreEmbeddedPluginJarProvider,
        IActiveSolutionBoundTracker activeSolutionBoundTracker, IConfigScopeUpdater configScopeUpdater, IThreadHandling threadHandling)
    {
        this.slCoreRpcFactory = slCoreRpcFactory;
        this.constantsProvider = constantsProvider;
        this.slCoreFoldersProvider = slCoreFoldersProvider;
        this.serverConnectionConfigurationProvider = serverConnectionConfigurationProvider;
        this.slCoreEmbeddedPluginJarProvider = slCoreEmbeddedPluginJarProvider;
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.configScopeUpdater = configScopeUpdater;
        this.threadHandling = threadHandling;
    }

    public async Task InitializeAsync()
    {
        threadHandling.ThrowIfOnUIThread();

        SLCoreRpc = slCoreRpcFactory.StartNewRpcInstance();

        if (!SLCoreRpc.ServiceProvider.TryGetTransientService(out ILifecycleManagementSLCoreService lifecycleManagementSlCoreService) ||
            !SLCoreRpc.ServiceProvider.TryGetTransientService(out ITelemetrySLCoreService telemetrySlCoreService))
        {
            throw new InvalidOperationException(SLCoreStrings.ServiceProviderNotInitialized);
        }

        var serverConnectionConfigurations = serverConnectionConfigurationProvider.GetServerConnections();
        var (storageRoot, workDir, sonarlintUserHome) = slCoreFoldersProvider.GetWorkFolders();

        await lifecycleManagementSlCoreService.InitializeAsync(new InitializeParams(
            constantsProvider.ClientConstants,
            new HttpConfigurationDto(new SslConfigurationDto()),
            constantsProvider.FeatureFlags,
            storageRoot,
            workDir,
            embeddedPluginPaths: slCoreEmbeddedPluginJarProvider.ListJarFiles(),
            connectedModeEmbeddedPluginPathsByKey: new Dictionary<string, string>(),
            enabledLanguagesInStandaloneMode: new List<Language>
            {
                Language.C,
                Language.CPP,
                Language.CS,
                Language.VBNET,
                Language.JS,
                Language.TS,
                Language.CSS,
                Language.SECRETS
            },
            extraEnabledLanguagesInConnectedMode: new List<Language>(),
            serverConnectionConfigurations.Values.OfType<SonarQubeConnectionConfigurationDto>().ToList(),
            serverConnectionConfigurations.Values.OfType<SonarCloudConnectionConfigurationDto>().ToList(),
            sonarlintUserHome,
            standaloneRuleConfigByKey: new Dictionary<string, StandaloneRuleConfigDto>(),
            isFocusOnNewCode: false,
            constantsProvider.TelemetryConstants,
            null));

        telemetrySlCoreService.DisableTelemetry();

        configScopeUpdater.UpdateConfigScopeForCurrentSolution(activeSolutionBoundTracker.CurrentConfiguration.Project);
    }


    public void Dispose()
    {
        if (SLCoreRpc?.ServiceProvider?.TryGetTransientService(out ILifecycleManagementSLCoreService lifecycleManagementSlCoreService) ?? false)
        {
            threadHandling.Run(async () =>
            {
                await lifecycleManagementSlCoreService.ShutdownAsync();
                return 0;
            });
        }

        SLCoreRpc?.Dispose();
    }
}
