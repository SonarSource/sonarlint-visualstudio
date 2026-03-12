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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Plugin;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.Integration.SupportedLanguages;

[Export(typeof(IPluginStatusesStore))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class PluginStatusesStore : IPluginStatusesStore, IDisposable
{
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly ISLCoreServiceProvider slCoreServiceProvider;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private readonly object lockObject = new();
    private List<PluginStatusDto> pluginStatuses = [];

    [ImportingConstructor]
    public PluginStatusesStore(
        IActiveConfigScopeTracker activeConfigScopeTracker,
        ISLCoreServiceProvider slCoreServiceProvider,
        IThreadHandling threadHandling,
        ILogger logger)
    {
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.slCoreServiceProvider = slCoreServiceProvider;
        this.threadHandling = threadHandling;
        this.logger = logger.ForVerboseContext(nameof(PluginStatusesStore));

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += OnCurrentConfigurationScopeChanged;
    }

    public IReadOnlyCollection<PluginStatusDto> GetAll()
    {
        lock (lockObject)
        {
            return pluginStatuses.ToList();
        }
    }

    public void Update(string configurationScopeId, IEnumerable<PluginStatusDto> newPluginStatuses)
    {
        lock (lockObject)
        {
            if (activeConfigScopeTracker.Current?.Id != configurationScopeId)
            {
                logger.LogVerbose(SLCoreStrings.ConfigurationScopeMismatch, configurationScopeId, activeConfigScopeTracker.Current?.Id);
                return;
            }

            pluginStatuses = newPluginStatuses.ToList();
        }

        RaisePluginStatusesChanged();
    }

    public event EventHandler PluginStatusesChanged;

    private void OnCurrentConfigurationScopeChanged(object sender, ConfigurationScopeChangedEventArgs e)
    {
        threadHandling.RunOnBackgroundThread(FetchPluginStatusesAsync).Forget();
    }

    private async Task FetchPluginStatusesAsync()
    {
        ConfigurationScope currentScope = activeConfigScopeTracker.Current;
        if (currentScope is not { Id: { } scopeId })
        {
            return;
        }

        if (!slCoreServiceProvider.TryGetTransientService(out IPluginSLCoreService pluginService))
        {
            logger.LogVerbose("SLCore service is not available");
            return;
        }

        try
        {
            GetPluginStatusesResponse response = await pluginService.GetPluginStatusesAsync(new GetPluginStatusesParams(scopeId));

            lock (lockObject)
            {
                if (activeConfigScopeTracker.Current?.Id != scopeId)
                {
                    return;
                }

                pluginStatuses = response.pluginStatuses ?? [];
            }

            RaisePluginStatusesChanged();
        }
        catch (Exception ex)
        {
            logger.LogVerbose("Failed to fetch plugin statuses: {0}", ex.Message);
        }
    }

    private void RaisePluginStatusesChanged() => PluginStatusesChanged?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        activeConfigScopeTracker.CurrentConfigurationScopeChanged -= OnCurrentConfigurationScopeChanged;
    }
}
