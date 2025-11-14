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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.EsLintBridge;
using SonarLint.VisualStudio.SLCore.NodeJS;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore;

internal interface ISLCoreInstanceFactory
{
    ISLCoreInstanceHandle CreateInstance();
}

[Export(typeof(ISLCoreInstanceFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class SLCoreInstanceFactory : ISLCoreInstanceFactory
{
    private readonly ISLCoreRpcFactory slCoreRpcFactory;
    private readonly ISLCoreRpcManager slCoreRpcManager;
    private readonly ISLCoreConstantsProvider constantsProvider;
    private readonly ISLCoreLanguageProvider slCoreLanguageProvider;
    private readonly ISLCoreFoldersProvider slCoreFoldersProvider;
    private readonly IServerConnectionsProvider serverConnectionConfigurationProvider;
    private readonly ISLCoreEmbeddedPluginProvider slCoreEmbeddedPluginProvider;
    private readonly INodeLocationProvider nodeLocator;
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly IConfigScopeUpdater configScopeUpdater;
    private readonly IThreadHandling threadHandling;
    private readonly ISLCoreRuleSettingsProvider slCoreRuleSettingsProvider;
    private readonly ISlCoreTelemetryMigrationProvider telemetryMigrationProvider;
    private readonly IEsLintBridgeLocator esLintBridgeLocator;

    [ImportingConstructor]
    public SLCoreInstanceFactory(
        ISLCoreRpcFactory slCoreRpcFactory,
        ISLCoreRpcManager slCoreRpcManager,
        ISLCoreConstantsProvider constantsProvider,
        ISLCoreLanguageProvider slCoreLanguageProvider,
        ISLCoreFoldersProvider slCoreFoldersProvider,
        IServerConnectionsProvider serverConnectionConfigurationProvider,
        ISLCoreEmbeddedPluginProvider slCoreEmbeddedPluginProvider,
        INodeLocationProvider nodeLocator,
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IConfigScopeUpdater configScopeUpdater,
        ISLCoreRuleSettingsProvider slCoreRuleSettingsProvider,
        ISlCoreTelemetryMigrationProvider telemetryMigrationProvider,
        IEsLintBridgeLocator esLintBridgeLocator,
        IThreadHandling threadHandling)
    {
        this.slCoreRpcFactory = slCoreRpcFactory;
        this.slCoreRpcManager = slCoreRpcManager;
        this.constantsProvider = constantsProvider;
        this.slCoreLanguageProvider = slCoreLanguageProvider;
        this.slCoreFoldersProvider = slCoreFoldersProvider;
        this.serverConnectionConfigurationProvider = serverConnectionConfigurationProvider;
        this.slCoreEmbeddedPluginProvider = slCoreEmbeddedPluginProvider;
        this.nodeLocator = nodeLocator;
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.configScopeUpdater = configScopeUpdater;
        this.slCoreRuleSettingsProvider = slCoreRuleSettingsProvider;
        this.telemetryMigrationProvider = telemetryMigrationProvider;
        this.esLintBridgeLocator = esLintBridgeLocator;
        this.threadHandling = threadHandling;
    }

    public ISLCoreInstanceHandle CreateInstance() =>
        new SLCoreInstanceHandle(
            slCoreRpcFactory,
            slCoreRpcManager,
            constantsProvider,
            slCoreLanguageProvider,
            slCoreFoldersProvider,
            serverConnectionConfigurationProvider,
            slCoreEmbeddedPluginProvider,
            nodeLocator,
            esLintBridgeLocator,
            activeSolutionBoundTracker,
            configScopeUpdater,
            slCoreRuleSettingsProvider,
            telemetryMigrationProvider,
            threadHandling);
}
