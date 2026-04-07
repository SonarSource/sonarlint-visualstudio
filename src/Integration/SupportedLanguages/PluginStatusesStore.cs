/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Integration.Resources;
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
    private readonly IPluginStatusDtoToPluginStatusDisplayConverter converter;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private readonly object lockObject = new();
    private ImmutableList<PluginStatusDisplay> pluginStatuses = ImmutableList<PluginStatusDisplay>.Empty;

    [ImportingConstructor]
    public PluginStatusesStore(
        IActiveConfigScopeTracker activeConfigScopeTracker,
        ISLCoreServiceProvider slCoreServiceProvider,
        IPluginStatusDtoToPluginStatusDisplayConverter converter,
        IThreadHandling threadHandling,
        ILogger logger)
    {
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.slCoreServiceProvider = slCoreServiceProvider;
        this.converter = converter;
        this.threadHandling = threadHandling;
        this.logger = logger.ForContext(Strings.PluginStatuses_LogContext);

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += OnCurrentConfigurationScopeChanged;
        threadHandling.RunOnBackgroundThread(FetchPluginStatusesAsync).Forget();
    }

    public IReadOnlyCollection<PluginStatusDisplay> GetAll()
    {
        lock (lockObject)
        {
            return pluginStatuses;
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

            pluginStatuses = newPluginStatuses.Select(converter.Convert).ToImmutableList();
        }

        RaisePluginStatusesChanged();
    }

    public void Clear()
    {
        lock (lockObject)
        {
            pluginStatuses = ImmutableList<PluginStatusDisplay>.Empty;
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
        if (!slCoreServiceProvider.TryGetTransientService(out IPluginSLCoreService pluginService))
        {
            logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
            return;
        }

        try
        {
            GetPluginStatusesResponse response = await pluginService.GetPluginStatusesAsync(new GetPluginStatusesParams(activeConfigScopeTracker.Current?.Id));

            lock (lockObject)
            {
                pluginStatuses = response.pluginStatuses.Select(converter.Convert).ToImmutableList();
            }

            RaisePluginStatusesChanged();
        }
        catch (Exception ex)
        {
            logger.WriteLine(Strings.PluginStatuses_FailedToFetchPluginStatuses, ex.Message);
        }
    }

    private void RaisePluginStatusesChanged() => PluginStatusesChanged?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        activeConfigScopeTracker.CurrentConfigurationScopeChanged -= OnCurrentConfigurationScopeChanged;
    }
}
