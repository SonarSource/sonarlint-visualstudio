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

internal interface ISLCoreInstanceHandle : IDisposable
{
    Task InitializeAsync();

    Task ShutdownTask { get; }
}

internal sealed class SLCoreInstanceHandle : ISLCoreInstanceHandle
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


    internal SLCoreInstanceHandle(ISLCoreRpcFactory slCoreRpcFactory, ISLCoreConstantsProvider constantsProvider,
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

        if (!SLCoreRpc.ServiceProvider.TryGetTransientService(out ILifecycleManagementSLCoreService lifecycleManagementSlCoreService))
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

        configScopeUpdater.UpdateConfigScopeForCurrentSolution(activeSolutionBoundTracker.CurrentConfiguration.Project);
    }


    public void Dispose()
    {
        if (SLCoreRpc?.ServiceProvider?.TryGetTransientService(out ILifecycleManagementSLCoreService lifecycleManagementSlCoreService) ?? false)
        {
            threadHandling.Run(async () =>
            {
                try
                {
                    await lifecycleManagementSlCoreService.ShutdownAsync();
                }
                catch (Exception)
                {
                    // suppressed
                }
                
                return 0;
            });
        }

        SLCoreRpc?.Dispose();
        SLCoreRpc = null;
    }
}
