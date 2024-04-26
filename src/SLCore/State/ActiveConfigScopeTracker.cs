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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Project;
using SonarLint.VisualStudio.SLCore.Service.Project.Models;
using SonarLint.VisualStudio.SLCore.Service.Project.Params;

namespace SonarLint.VisualStudio.SLCore.State;

public interface IActiveConfigScopeTracker : IDisposable
{
    ConfigurationScope Current { get; }

    void SetCurrentConfigScope(string id, string connectionId = null, string sonarProjectKey = null);

    void Reset();

    void RemoveCurrentConfigScope();

    void UpdateRootOnCurrentConfigScope(string root);
}

public record ConfigurationScope(
    string Id,
    string ConnectionId = null,
    string SonarProjectId = null,
    string RootPath = null)
{
    public string Id { get; } = Id ?? throw new ArgumentNullException(nameof(Id));
}

[Export(typeof(IActiveConfigScopeTracker))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class ActiveConfigScopeTracker : IActiveConfigScopeTracker
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IThreadHandling threadHandling;
    private readonly IAsyncLock asyncLock;

    internal /* for testing */ ConfigurationScopeDto currentConfigScope;

    [ImportingConstructor]
    public ActiveConfigScopeTracker(ISLCoreServiceProvider serviceProvider,
        IAsyncLockFactory asyncLockFactory,
        IThreadHandling threadHandling)
    {
        this.serviceProvider = serviceProvider;
        this.threadHandling = threadHandling;
        asyncLock = asyncLockFactory.Create();
    }

    public ConfigurationScope Current
    {
        get
        {
            threadHandling.ThrowIfOnUIThread();

            using (asyncLock.Acquire())
                return currentConfigScope is not null
                    ? new ConfigurationScope(currentConfigScope.id,
                        currentConfigScope.binding?.connectionId,
                        currentConfigScope.binding?.sonarProjectKey)
                    : null;
        }
    }

    public void SetCurrentConfigScope(string id, string connectionId = null, string sonarProjectKey = null)
    {
        threadHandling.ThrowIfOnUIThread();

        if (!serviceProvider.TryGetTransientService(out IConfigurationScopeSLCoreService configurationScopeService))
        {
            throw new InvalidOperationException(SLCoreStrings.ServiceProviderNotInitialized);
        }

        var configurationScopeDto = new ConfigurationScopeDto(id,
            id,
            true,
            connectionId is not null ? new BindingConfigurationDto(connectionId, sonarProjectKey) : null);

        using (asyncLock.Acquire())
        {
            Debug.Assert(currentConfigScope == null || currentConfigScope.id == id, "Config scope conflict");

            if (currentConfigScope?.id == id)
            {
                configurationScopeService.DidUpdateBinding(new DidUpdateBindingParams(id, configurationScopeDto.binding));
            }
            else
            {
                configurationScopeService.DidAddConfigurationScopes(
                    new DidAddConfigurationScopesParams(new List<ConfigurationScopeDto> { configurationScopeDto }));
            }
            currentConfigScope = configurationScopeDto;
        }
    }

    public void Reset()
    {
        threadHandling.ThrowIfOnUIThread();

        using (asyncLock.Acquire())
        {
            currentConfigScope = null;
        }
    }

    public void RemoveCurrentConfigScope()
    {
        threadHandling.ThrowIfOnUIThread();

        if (!serviceProvider.TryGetTransientService(out IConfigurationScopeSLCoreService configurationScopeService))
        {
            throw new InvalidOperationException(SLCoreStrings.ServiceProviderNotInitialized);
        }

        using (asyncLock.Acquire())
        {
            if (currentConfigScope is null)
            {
                return;
            }

            configurationScopeService.DidRemoveConfigurationScope(
                new DidRemoveConfigurationScopeParams(currentConfigScope.id));
            currentConfigScope = null;
        }
    }

    public void Dispose()
    {
        asyncLock?.Dispose();
    }

    public void UpdateRootOnCurrentConfigScope(string root)
    {
        //will be implemented in seperate PR
        throw new NotImplementedException();
    }
}
