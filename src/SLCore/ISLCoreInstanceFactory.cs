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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Configuration;
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
    private readonly ISLCoreConstantsProvider constantsProvider;
    private readonly ISLCoreFoldersProvider slCoreFoldersProvider;
    private readonly IServerConnectionsProvider serverConnectionConfigurationProvider;
    private readonly ISLCoreEmbeddedPluginJarLocator slCoreEmbeddedPluginJarProvider;
    private readonly INodeLocationProvider nodeLocator;
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly IConfigScopeUpdater configScopeUpdater;
    private readonly IThreadHandling threadHandling;
    private readonly ISLCoreRuleSettings ruleSettings;

    [ImportingConstructor]
    public SLCoreInstanceFactory(ISLCoreRpcFactory slCoreRpcFactory,
        ISLCoreConstantsProvider constantsProvider,
        ISLCoreFoldersProvider slCoreFoldersProvider,
        IServerConnectionsProvider serverConnectionConfigurationProvider,
        ISLCoreEmbeddedPluginJarLocator slCoreEmbeddedPluginJarProvider,
        INodeLocationProvider nodeLocator,
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IConfigScopeUpdater configScopeUpdater,
        IThreadHandling threadHandling,
        ISLCoreRuleSettings ruleSettings)
    {
        this.slCoreRpcFactory = slCoreRpcFactory;
        this.constantsProvider = constantsProvider;
        this.slCoreFoldersProvider = slCoreFoldersProvider;
        this.serverConnectionConfigurationProvider = serverConnectionConfigurationProvider;
        this.slCoreEmbeddedPluginJarProvider = slCoreEmbeddedPluginJarProvider;
        this.nodeLocator = nodeLocator;
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.configScopeUpdater = configScopeUpdater;
        this.threadHandling = threadHandling;
        this.ruleSettings = ruleSettings;
    }
    
    public ISLCoreInstanceHandle CreateInstance() =>
        new SLCoreInstanceHandle(slCoreRpcFactory,
            constantsProvider,
            slCoreFoldersProvider,
            serverConnectionConfigurationProvider,
            slCoreEmbeddedPluginJarProvider,
            nodeLocator,
            activeSolutionBoundTracker,
            configScopeUpdater,
            threadHandling,
            ruleSettings);
}
