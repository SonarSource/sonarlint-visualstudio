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

using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Analysis;
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
    Task<Task> InitializeAsync();
}

internal sealed class SLCoreInstanceHandle(
    ISLCoreRpcFactory slCoreRpcFactory,
    ISLCoreRpcManager slCoreRpcManager,
    ISLCoreConstantsProvider constantsProvider,
    ISLCoreLanguageProvider slCoreLanguageProvider,
    ISLCoreFoldersProvider slCoreFoldersProvider,
    IServerConnectionsProvider serverConnectionConfigurationProvider,
    ISLCoreEmbeddedPluginProvider slCoreEmbeddedPluginJarProvider,
    INodeLocationProvider nodeLocator,
    IEsLintBridgeLocator esLintBridgeLocator,
    IActiveSolutionBoundTracker activeSolutionBoundTracker,
    IConfigScopeUpdater configScopeUpdater,
    ISLCoreRuleSettingsProvider slCoreRuleSettingsProvider,
    ISlCoreTelemetryMigrationProvider telemetryMigrationProvider,
    IFocusOnNewCodeService focusOnNewCodeService,
    IThreadHandling threadHandling) : ISLCoreInstanceHandle
{
    private ISLCoreRpc? slCoreRpc;

    public async Task<Task> InitializeAsync()
    {
        threadHandling.ThrowIfOnUIThread();

        await focusOnNewCodeService.InitializationProcessor.InitializeAsync();

        slCoreRpc = slCoreRpcFactory.StartNewRpcInstance();

        var serverConnectionConfigurations = serverConnectionConfigurationProvider.GetServerConnections();
        var (storageRoot, workDir, sonarlintUserHome) = slCoreFoldersProvider.GetWorkFolders();

        var initializationParams = new InitializeParams(
            constantsProvider.ClientConstants,
            new HttpConfigurationDto(new SslConfigurationDto()),
            constantsProvider.BackendCapabilities,
            storageRoot,
            workDir,
            embeddedPluginPaths: slCoreEmbeddedPluginJarProvider.ListJarFiles(),
            connectedModeEmbeddedPluginPathsByKey: slCoreEmbeddedPluginJarProvider.ListConnectedModeEmbeddedPluginPathsByKey(),
            enabledLanguagesInStandaloneMode: slCoreLanguageProvider.LanguagesInStandaloneMode,
            extraEnabledLanguagesInConnectedMode: slCoreLanguageProvider.ExtraLanguagesInConnectedMode,
            disabledPluginKeysForAnalysis: slCoreEmbeddedPluginJarProvider.ListDisabledPluginKeysForAnalysis(),
            serverConnectionConfigurations.Values.OfType<SonarQubeConnectionConfigurationDto>().ToList(),
            serverConnectionConfigurations.Values.OfType<SonarCloudConnectionConfigurationDto>().ToList(),
            sonarlintUserHome,
            standaloneRuleConfigByKey: slCoreRuleSettingsProvider.GetSLCoreRuleSettings(),
            isFocusOnNewCode: focusOnNewCodeService.Current.IsEnabled,
            constantsProvider.TelemetryConstants,
            telemetryMigrationProvider.Get(),
            new LanguageSpecificRequirements(new JsTsRequirementsDto(nodeLocator.Get(), esLintBridgeLocator.Get())),
            automaticAnalysisEnabled: true);

        slCoreRpcManager.Initialize(initializationParams);

        await UpdateConfigurationScopeForCurrentSolutionAsync();

        return slCoreRpc?.ShutdownTask ?? Task.CompletedTask;
    }

    private async Task UpdateConfigurationScopeForCurrentSolutionAsync()
    {
        // this case does not follow the pattern of implementing the IRequireInitialization as it would not be worth the effort
        await activeSolutionBoundTracker.InitializationProcessor.InitializeAsync();
        configScopeUpdater.UpdateConfigScopeForCurrentSolution(activeSolutionBoundTracker.CurrentConfiguration.Project);
    }

    public void Dispose()
    {
        Shutdown();
        slCoreRpc?.Dispose();
        slCoreRpc = null;
    }

    private void Shutdown()
    {
        try
        {
            threadHandling.Run(async () =>
            {
                await threadHandling.SwitchToBackgroundThread();
                slCoreRpcManager.Shutdown();

                return 0;
            });
        }
        catch
        {
            // ignore
        }
    }
}
