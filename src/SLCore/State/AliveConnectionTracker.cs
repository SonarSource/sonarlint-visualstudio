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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.State;

/// <summary>
/// Handles connection list refreshing integration with SLCore
/// </summary>
public interface IAliveConnectionTracker : IDisposable
{
    void RefreshConnectionList();
}

[Export(typeof(IAliveConnectionTracker))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class AliveConnectionTracker : IAliveConnectionTracker
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IThreadHandling threadHandling;
    private readonly IAsyncLock asyncLock;
    private readonly IServerConnectionsProvider serverConnectionsProvider;
    private readonly ISolutionBindingRepository solutionBindingRepository;

    [ImportingConstructor]
    public AliveConnectionTracker(ISLCoreServiceProvider serviceProvider,
        IServerConnectionsProvider serverConnectionsProvider,
        ISolutionBindingRepository solutionBindingRepository,
        IAsyncLockFactory asyncLockFactory,
        IThreadHandling threadHandling)
    {
        this.serviceProvider = serviceProvider;
        this.serverConnectionsProvider = serverConnectionsProvider;
        this.threadHandling = threadHandling;
        this.solutionBindingRepository = solutionBindingRepository;
        this.solutionBindingRepository.BindingUpdated += BindingUpdateHandler;
        asyncLock = asyncLockFactory.Create();
    }

    public void RefreshConnectionList()
    {
        threadHandling.ThrowIfOnUIThread();

        if (!serviceProvider.TryGetTransientService(out IConnectionConfigurationSLCoreService connectionConfigurationService))
        {
            throw new InvalidOperationException(SLCoreStrings.ServiceProviderNotInitialized);
        }

        using (asyncLock.Acquire())
        {
            var serverConnections = serverConnectionsProvider.GetServerConnections();

            connectionConfigurationService.DidUpdateConnections(new DidUpdateConnectionsParams(
                serverConnections.Values.OfType<SonarQubeConnectionConfigurationDto>().ToList(),
                serverConnections.Values.OfType<SonarCloudConnectionConfigurationDto>().ToList()));

            foreach (var connectionId in serverConnections.Keys)
            {
                // we don't manage connections as separate entities and we don't know when credentials actually change
                connectionConfigurationService.DidChangeCredentials(
                    new DidChangeCredentialsParams(connectionId));
            }
        }
    }

    private void BindingUpdateHandler(object sender, EventArgs arg)
    {
        threadHandling.RunOnBackgroundThread(() =>
        {
            RefreshConnectionList();

            return Task.FromResult(0);
        }).Forget();
    }

    public void Dispose()
    {
        solutionBindingRepository.BindingUpdated -= BindingUpdateHandler;
        asyncLock.Dispose();
    }
}
