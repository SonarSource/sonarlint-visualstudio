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
using System.Diagnostics;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Project;
using SonarLint.VisualStudio.SLCore.Service.Project.Models;

namespace SonarLint.VisualStudio.SLCore.State;

public interface IActiveConfigScopeTracker : IDisposable
{
    ConfigurationScope Current { get; }
    Task SetCurrentConfigScopeAsync(string id, string connectionId = null, string sonarProjectKey = null);
    Task RemoveCurrentConfigScopeAsync();
}

public class ConfigurationScope
{
    public ConfigurationScope(string id, string connectionId = null, string sonarProjectId = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        ConnectionId = connectionId;
        SonarProjectId = sonarProjectId;
    }

    public string Id { get; }
    public string ConnectionId { get; }
    public string SonarProjectId { get; }
}

[Export(typeof(IActiveConfigScopeTracker))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class ActiveConfigScopeTracker : IActiveConfigScopeTracker
{
    private readonly IAsyncLock asyncLock;

    private readonly ISLCoreServiceProvider serviceProvider;

    internal /* for testing */ ConfigurationScopeDto currentConfigScope;

    [ImportingConstructor]
    public ActiveConfigScopeTracker(ISLCoreServiceProvider serviceProvider,
        IAsyncLockFactory asyncLockFactory)
    {
        this.serviceProvider = serviceProvider;
        asyncLock = asyncLockFactory.Create();
    }

    public ConfigurationScope Current
    {
        get
        {
            using (asyncLock.Acquire())
                return currentConfigScope is not null
                    ? new ConfigurationScope(currentConfigScope.id,
                        currentConfigScope.binding?.connectionId,
                        currentConfigScope.binding?.sonarProjectKey)
                    : null;
        }
    }
    
    public async Task SetCurrentConfigScopeAsync(string id, string connectionId = null, string sonarProjectKey = null)
    {
        if (!serviceProvider.TryGetTransientService(out IConfigurationScopeSLCoreService configurationScopeService)
            || !serviceProvider.TryGetTransientService(out IBindingSLCoreService bindingSlCoreService))
        {
            throw new InvalidOperationException(Strings.ServiceProviderNotInitialized);
        }

        var configurationScopeDto = new ConfigurationScopeDto(id,
            id,
            true,
            connectionId is not null ? new BindingConfigurationDto(connectionId, sonarProjectKey) : null);

        using (await asyncLock.AcquireAsync())
        {
            if (currentConfigScope.id == id)
            {
                await bindingSlCoreService.DidUpdateBindingAsync(new DidUpdateBindingParams(id, configurationScopeDto.binding));
            }
            else
            {
                await configurationScopeService.DidAddConfigurationScopesAsync(
                    new DidAddConfigurationScopesParams(new List<ConfigurationScopeDto> { configurationScopeDto }));
            }
            currentConfigScope = configurationScopeDto;
        }
    }


    public async Task RemoveCurrentConfigScopeAsync()
    {
        if (!serviceProvider.TryGetTransientService(out IConfigurationScopeSLCoreService configurationScopeService))
        {
            throw new InvalidOperationException(Strings.ServiceProviderNotInitialized);
        }

        using (await asyncLock.AcquireAsync())
        {
            Debug.Assert(currentConfigScope != null);

            await configurationScopeService.DidRemoveConfigurationScopeAsync(
                new DidRemoveConfigurationScopeParams(currentConfigScope.id));
            currentConfigScope = null;
        }
    }

    public void Dispose()
    {
        asyncLock?.Dispose();
    }
}
