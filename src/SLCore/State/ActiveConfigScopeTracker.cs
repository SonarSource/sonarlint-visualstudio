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
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Project;
using SonarLint.VisualStudio.SLCore.Service.Project.Models;
using SonarLint.VisualStudio.SLCore.Service.Project.Params;

namespace SonarLint.VisualStudio.SLCore.State;

[Export(typeof(IActiveConfigScopeTracker))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class ActiveConfigScopeTracker : IActiveConfigScopeTracker
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IThreadHandling threadHandling;
    private readonly IAsyncLock asyncLock;

    internal /* for testing */ ConfigurationScope currentConfigScope;

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
                return currentConfigScope;
        }
    }

    public void SetCurrentConfigScope(string id, string connectionId = null, string sonarProjectKey = null)
    {
        threadHandling.ThrowIfOnUIThread();

        if (!serviceProvider.TryGetTransientService(out IConfigurationScopeSLCoreService configurationScopeService))
        {
            throw new InvalidOperationException(SLCoreStrings.ServiceProviderNotInitialized);
        }

        using (asyncLock.Acquire())
        {
            if (currentConfigScope != null && currentConfigScope.Id != id)
            {
                Debug.Assert(true, "Config scope conflict");
                throw new InvalidOperationException(SLCoreStrings.ConfigScopeConflict);
            }

            if (currentConfigScope != null)
            {
                configurationScopeService.DidUpdateBinding(new DidUpdateBindingParams(id, GetBinding(connectionId, sonarProjectKey)));
                currentConfigScope = currentConfigScope with { ConnectionId = connectionId, SonarProjectId = sonarProjectKey };
            }
            else
            {
                configurationScopeService.DidAddConfigurationScopes(new DidAddConfigurationScopesParams([
                    new ConfigurationScopeDto(id, id, true, GetBinding(connectionId, sonarProjectKey))]));
                currentConfigScope = new ConfigurationScope(id, connectionId, sonarProjectKey);
            }
        }

        OnCurrentConfigurationScopeChanged();
    }

    public void Reset()
    {
        threadHandling.ThrowIfOnUIThread();
        using (asyncLock.Acquire())
        {
            currentConfigScope = null;
        }
        OnCurrentConfigurationScopeChanged();
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
                new DidRemoveConfigurationScopeParams(currentConfigScope.Id));
            currentConfigScope = null;
        }

        OnCurrentConfigurationScopeChanged();
    }

    public bool TryUpdateRootOnCurrentConfigScope(string id, string root)
    {
        using (asyncLock.Acquire())
        {
            if (id is null || currentConfigScope?.Id != id)
            {
                return false;
            }

            currentConfigScope = currentConfigScope with { RootPath = root };
        }
        OnCurrentConfigurationScopeChanged();
        return true;
    }

    public bool TryUpdateAnalysisReadinessOnCurrentConfigScope(string id, bool isReady)
    {
        using (asyncLock.Acquire())
        {
            if (id is null || currentConfigScope?.Id != id)
            {
                return false;
            }
            
            currentConfigScope = currentConfigScope with { IsReadyForAnalysis = isReady};
        }
        OnCurrentConfigurationScopeChanged();
        return true;
    }

    public event EventHandler CurrentConfigurationScopeChanged;

    public void Dispose()
    {
        asyncLock?.Dispose();
    }

    private BindingConfigurationDto GetBinding(string connectionId, string sonarProjectKey) => connectionId is not null
        ? new BindingConfigurationDto(connectionId, sonarProjectKey)
        : null;

    private void OnCurrentConfigurationScopeChanged()
    {
        CurrentConfigurationScopeChanged?.Invoke(this, EventArgs.Empty);
    }
}
