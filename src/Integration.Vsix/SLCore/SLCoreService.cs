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
using SonarLint.VisualStudio.Integration.Vsix.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using SonarLint.VisualStudio.SLCore.State;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.Integration.Vsix.SLCore;

internal class SLCoreService : IDisposable
{
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly ISLCoreRpcFactory slCoreRpcFactory;
    private readonly IServerConnectionsProvider serverConnectionConfigurationProvider;
    private readonly IConfigScopeUpdater configScopeUpdater;
    private readonly ISLCoreConstantsProvider constantsProvider;
    private readonly ISLCoreFoldersProvider slCoreFoldersProvider;
    private readonly ISLCoreEmbeddedPluginJarLocator slCoreEmbeddedPluginJarProvider;
    private readonly IThreadHandling threadHandling;
    private ISLCoreRpc slCoreRpc;


    public SLCoreService(ISLCoreRpcFactory slCoreRpcFactory, ISLCoreConstantsProvider constantsProvider, ISLCoreFoldersProvider slCoreFoldersProvider,
        IServerConnectionsProvider serverConnectionConfigurationProvider, ISLCoreEmbeddedPluginJarLocator slCoreEmbeddedPluginJarProvider,
        IActiveSolutionBoundTracker activeSolutionBoundTracker, IConfigScopeUpdater configScopeUpdater, IThreadHandling threadHandling)
    {
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.slCoreRpcFactory = slCoreRpcFactory;
        this.serverConnectionConfigurationProvider = serverConnectionConfigurationProvider;
        this.configScopeUpdater = configScopeUpdater;
        this.constantsProvider = constantsProvider;
        this.slCoreFoldersProvider = slCoreFoldersProvider;
        this.slCoreEmbeddedPluginJarProvider = slCoreEmbeddedPluginJarProvider;
        this.threadHandling = threadHandling;
    }

    public Task ShutdownTask => slCoreRpc.ShutdownTask;

    public async Task Initialize()
    {
        threadHandling.ThrowIfOnUIThread();
        
        slCoreRpc = slCoreRpcFactory.StartNewRpcInstance();

        if (!slCoreRpc.ServiceProvider.TryGetTransientService(out ILifecycleManagementSLCoreService lifecycleManagementSlCoreService) ||
            !slCoreRpc.ServiceProvider.TryGetTransientService(out ITelemetrySLCoreService telemetrySlCoreService))
        {
            throw new InvalidOperationException();
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
        if (slCoreRpc?.ServiceProvider?.TryGetTransientService(out ILifecycleManagementSLCoreService lifecycleManagementSlCoreService) ?? false)
        {
            threadHandling.Run(async () =>
            {
                await lifecycleManagementSlCoreService.ShutdownAsync();
                return 0;
            });
        }

        slCoreRpc?.Dispose();
    }
}
