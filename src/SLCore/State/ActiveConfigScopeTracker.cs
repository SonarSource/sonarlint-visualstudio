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
[method: ImportingConstructor]
internal sealed class ActiveConfigScopeTracker(
    ISLCoreServiceProvider serviceProvider,
    IAsyncLockFactory asyncLockFactory,
    IThreadHandling threadHandling,
    ILogger logger)
    : IActiveConfigScopeTracker
{
    private readonly ILogger logger = logger.ForContext(SLCoreStrings.SLCoreName, SLCoreStrings.ConfigurationScope_LogContext);
    private readonly IAsyncLock asyncLock = asyncLockFactory.Create();

    internal /* for testing */ ConfigurationScope? CurrentConfigScope;

    public ConfigurationScope? Current
    {
        get
        {
            threadHandling.ThrowIfOnUIThread();

            using (asyncLock.Acquire())
            {
                return CurrentConfigScope;
            }
        }
    }

    public void SetCurrentConfigScope(string id, string? connectionId = null, string? sonarProjectKey = null)
    {
        threadHandling.ThrowIfOnUIThread();

        bool declarationChanged;

        if (!serviceProvider.TryGetTransientService(out IConfigurationScopeSLCoreService? configurationScopeService))
        {
            throw new InvalidOperationException(SLCoreStrings.ServiceProviderNotInitialized);
        }

        using (asyncLock.Acquire())
        {
            if (CurrentConfigScope != null && CurrentConfigScope.Id != id)
            {
                Debug.Assert(true, "Config scope conflict");
                throw new InvalidOperationException(SLCoreStrings.ConfigScopeConflict);
            }

            var bindingConfigurationDto = GetBinding(connectionId, sonarProjectKey);

            if (CurrentConfigScope != null)
            {
                declarationChanged = false;
                configurationScopeService.DidUpdateBinding(new DidUpdateBindingParams(id, bindingConfigurationDto));
                CurrentConfigScope = CurrentConfigScope with { ConnectionId = connectionId, SonarProjectId = sonarProjectKey };
                logger.WriteLine(SLCoreStrings.ConfigScope_UpdatedBinding, id);
            }
            else
            {
                declarationChanged = true;
                configurationScopeService.DidAddConfigurationScopes(new DidAddConfigurationScopesParams([
                    new ConfigurationScopeDto(id, id, true, bindingConfigurationDto)]));
                CurrentConfigScope = new ConfigurationScope(id, connectionId, sonarProjectKey);
                logger.WriteLine(SLCoreStrings.ConfigScope_Declared, id);
            }
            LogConfigurationScopeChangedUnsafe();
        }

        OnCurrentConfigurationScopeChanged(declarationChanged);
    }

    public void Reset()
    {
        threadHandling.ThrowIfOnUIThread();
        using (asyncLock.Acquire())
        {
            logger.WriteLine(SLCoreStrings.ConfigScope_Reset);
            CurrentConfigScope = null;
            LogConfigurationScopeChangedUnsafe();
        }
        OnCurrentConfigurationScopeChanged(true);
    }

    public void RemoveCurrentConfigScope()
    {
        threadHandling.ThrowIfOnUIThread();

        if (!serviceProvider.TryGetTransientService(out IConfigurationScopeSLCoreService? configurationScopeService))
        {
            throw new InvalidOperationException(SLCoreStrings.ServiceProviderNotInitialized);
        }

        using (asyncLock.Acquire())
        {
            if (CurrentConfigScope is null)
            {
                return;
            }

            configurationScopeService.DidRemoveConfigurationScope(
                new DidRemoveConfigurationScopeParams(CurrentConfigScope.Id));
            logger.WriteLine(SLCoreStrings.ConfigScope_Removed, CurrentConfigScope.Id);
            CurrentConfigScope = null;
            LogConfigurationScopeChangedUnsafe();
        }

        OnCurrentConfigurationScopeChanged(true);
    }

    public bool TryUpdateRootOnCurrentConfigScope(string? id, string root, string commandsBaseDir)
    {
        using (asyncLock.Acquire())
        {
            if (id is null || CurrentConfigScope?.Id != id)
            {
                return false;
            }

            CurrentConfigScope = CurrentConfigScope with { RootPath = root, CommandsBaseDir = commandsBaseDir };
            logger.WriteLine(SLCoreStrings.ConfigScope_UpdatedFileSystem, id, root, commandsBaseDir);
            LogConfigurationScopeChangedUnsafe();
        }
        OnCurrentConfigurationScopeChanged(false);
        return true;
    }

    public bool TryUpdateAnalysisReadinessOnCurrentConfigScope(string? id, bool isReady)
    {
        using (asyncLock.Acquire())
        {
            if (id is null || CurrentConfigScope?.Id != id)
            {
                return false;
            }

            CurrentConfigScope = CurrentConfigScope with { IsReadyForAnalysis = isReady};
            logger.WriteLine(SLCoreStrings.ConfigScope_UpdatedAnalysisReadiness, id, isReady);
            LogConfigurationScopeChangedUnsafe();
        }
        OnCurrentConfigurationScopeChanged(false);
        return true;
    }

    public bool TryUpdateMatchedBranchOnCurrentConfigScope(string? id, string branch)
    {
        using (asyncLock.Acquire())
        {
            if (id is null || CurrentConfigScope?.Id != id)
            {
                return false;
            }

            CurrentConfigScope = CurrentConfigScope with { MatchedBranch = branch};
            logger.WriteLine(SLCoreStrings.ConfigScope_UpdatedAnalysisReadiness, id, branch);
            LogConfigurationScopeChangedUnsafe();
        }
        OnCurrentConfigurationScopeChanged(false);
        return true;
    }

    public event EventHandler<ConfigurationScopeChangedEventArgs>? CurrentConfigurationScopeChanged;

    public void Dispose() =>
        asyncLock?.Dispose();

    private static BindingConfigurationDto? GetBinding(string? connectionId, string? sonarProjectKey) => connectionId is not null
        ? new BindingConfigurationDto(connectionId, sonarProjectKey)
        : null;

    private void LogConfigurationScopeChangedUnsafe() =>
        logger.LogVerbose(SLCoreStrings.ConfigurationScopeChanged, CurrentConfigScope);

    private void OnCurrentConfigurationScopeChanged(bool declarationChanged) =>
        CurrentConfigurationScopeChanged?.Invoke(this, new (declarationChanged));
}
