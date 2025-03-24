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
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore;

internal interface ISLCoreInstanceHandle : IDisposable
{
    void Initialize();

    Task ShutdownTask { get; }
}

internal sealed class SLCoreInstanceHandle : ISLCoreInstanceHandle
{
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly ISLCoreRpcFactory slCoreRpcFactory;
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IServerConnectionsProvider serverConnectionConfigurationProvider;
    private readonly IConfigScopeUpdater configScopeUpdater;
    private readonly ISLCoreConstantsProvider constantsProvider;
    private readonly ISLCoreLanguageProvider slCoreLanguageProvider;
    private readonly ISLCoreFoldersProvider slCoreFoldersProvider;
    private readonly ISLCoreEmbeddedPluginJarLocator slCoreEmbeddedPluginJarProvider;
    private readonly ISLCoreRuleSettingsProvider slCoreRuleSettingsProvider;
    private readonly ISlCoreTelemetryMigrationProvider telemetryMigrationProvider;
    private readonly IEsLintBridgeLocator esLintBridgeLocator;
    private readonly INodeLocationProvider nodeLocator;
    private readonly IThreadHandling threadHandling;
    public Task ShutdownTask => SLCoreRpc.ShutdownTask;
    private ISLCoreRpc SLCoreRpc { get; set; }

    internal SLCoreInstanceHandle(
        ISLCoreRpcFactory slCoreRpcFactory,
        ISLCoreServiceProvider serviceProvider,
        ISLCoreConstantsProvider constantsProvider,
        ISLCoreLanguageProvider slCoreLanguageProvider,
        ISLCoreFoldersProvider slCoreFoldersProvider,
        IServerConnectionsProvider serverConnectionConfigurationProvider,
        ISLCoreEmbeddedPluginJarLocator slCoreEmbeddedPluginJarProvider,
        INodeLocationProvider nodeLocator,
        IEsLintBridgeLocator esLintBridgeLocator,
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IConfigScopeUpdater configScopeUpdater,
        ISLCoreRuleSettingsProvider slCoreRuleSettingsProvider,
        ISlCoreTelemetryMigrationProvider telemetryMigrationProvider,
        IThreadHandling threadHandling)
    {
        this.slCoreRpcFactory = slCoreRpcFactory;
        this.serviceProvider = serviceProvider;
        this.constantsProvider = constantsProvider;
        this.slCoreLanguageProvider = slCoreLanguageProvider;
        this.slCoreFoldersProvider = slCoreFoldersProvider;
        this.serverConnectionConfigurationProvider = serverConnectionConfigurationProvider;
        this.slCoreEmbeddedPluginJarProvider = slCoreEmbeddedPluginJarProvider;
        this.nodeLocator = nodeLocator;
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.configScopeUpdater = configScopeUpdater;
        this.threadHandling = threadHandling;
        this.slCoreRuleSettingsProvider = slCoreRuleSettingsProvider;
        this.telemetryMigrationProvider = telemetryMigrationProvider;
        this.esLintBridgeLocator = esLintBridgeLocator;
    }

    public void Initialize()
    {
        threadHandling.ThrowIfOnUIThread();

        SLCoreRpc = slCoreRpcFactory.StartNewRpcInstance();

        if (!serviceProvider.TryGetTransientService(out ILifecycleManagementSLCoreService lifecycleManagementSlCoreService))
        {
            throw new InvalidOperationException(SLCoreStrings.ServiceProviderNotInitialized);
        }

        var serverConnectionConfigurations = serverConnectionConfigurationProvider.GetServerConnections();
        var (storageRoot, workDir, sonarlintUserHome) = slCoreFoldersProvider.GetWorkFolders();

        lifecycleManagementSlCoreService.Initialize(new InitializeParams(
            constantsProvider.ClientConstants,
            new HttpConfigurationDto(new SslConfigurationDto()),
            constantsProvider.FeatureFlags,
            storageRoot,
            workDir,
            embeddedPluginPaths: slCoreEmbeddedPluginJarProvider.ListJarFiles(),
            connectedModeEmbeddedPluginPathsByKey: slCoreEmbeddedPluginJarProvider.ListConnectedModeEmbeddedPluginPathsByKey(),
            enabledLanguagesInStandaloneMode: slCoreLanguageProvider.LanguagesInStandaloneMode,
            extraEnabledLanguagesInConnectedMode: slCoreLanguageProvider.ExtraLanguagesInConnectedMode,
            disabledPluginKeysForAnalysis: slCoreLanguageProvider.LanguagesWithDisabledAnalysis.Select(l => l.GetPluginKey()).ToList(),
            serverConnectionConfigurations.Values.OfType<SonarQubeConnectionConfigurationDto>().ToList(),
            serverConnectionConfigurations.Values.OfType<SonarCloudConnectionConfigurationDto>().ToList(),
            sonarlintUserHome,
            standaloneRuleConfigByKey: slCoreRuleSettingsProvider.GetSLCoreRuleSettings(),
            isFocusOnNewCode: false,
            constantsProvider.TelemetryConstants,
            telemetryMigrationProvider.Get(),
            new LanguageSpecificRequirements(new JsTsRequirementsDto(nodeLocator.Get(), esLintBridgeLocator.Get()))));

        configScopeUpdater.UpdateConfigScopeForCurrentSolution(activeSolutionBoundTracker.CurrentConfiguration.Project);
    }

    public void Dispose()
    {
        Shutdown();
        SLCoreRpc?.Dispose();
        SLCoreRpc = null;
    }

    private void Shutdown()
    {
        try
        {
            threadHandling.Run(async () =>
            {
                await threadHandling.SwitchToBackgroundThread();
                if (serviceProvider.TryGetTransientService(out ILifecycleManagementSLCoreService lifecycleManagementSlCoreService))
                {
                    lifecycleManagementSlCoreService.Shutdown();
                }

                return 0;
            });
        }
        catch
        {
            // ignore
        }
    }
}
