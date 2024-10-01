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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.State;

/// <summary>
/// Handles connection list refreshing integration with SLCore
/// </summary>
public interface IAliveConnectionTracker : IDisposable;

[Export(typeof(IAliveConnectionTracker))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class AliveConnectionTracker : IAliveConnectionTracker
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IThreadHandling threadHandling;
    private readonly IAsyncLock asyncLock;
    private readonly IServerConnectionsProvider serverConnectionsProvider;
    private readonly IServerConnectionsRepository serverConnectionsRepository;

    [ImportingConstructor]
    public AliveConnectionTracker(ISLCoreServiceProvider serviceProvider,
        IServerConnectionsProvider serverConnectionsProvider,
        IServerConnectionsRepository serverConnectionsRepository,
        IAsyncLockFactory asyncLockFactory,
        IThreadHandling threadHandling)
    {
        this.serviceProvider = serviceProvider;
        this.serverConnectionsProvider = serverConnectionsProvider;
        this.threadHandling = threadHandling;
        this.serverConnectionsRepository = serverConnectionsRepository;
        this.serverConnectionsRepository.ConnectionChanged += ConnectionUpdateHandler;
        this.serverConnectionsRepository.CredentialsChanged += CredentialsUpdateHandler;
        asyncLock = asyncLockFactory.Create();
    }

    internal void RefreshConnectionList()
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
                connectionConfigurationService.DidChangeCredentials(new DidChangeCredentialsParams(connectionId));
            }
        }
    }

    internal void UpdateCredentials(string connectionId)
    {
        threadHandling.ThrowIfOnUIThread();

        if (!serviceProvider.TryGetTransientService(out IConnectionConfigurationSLCoreService connectionConfigurationService))
        {
            throw new InvalidOperationException(SLCoreStrings.ServiceProviderNotInitialized);
        }

        using (asyncLock.Acquire())
        {
            connectionConfigurationService.DidChangeCredentials(new DidChangeCredentialsParams(connectionId));
        }
    }

    private void ConnectionUpdateHandler(object sender, EventArgs arg)
    {
        threadHandling.RunOnBackgroundThread(() =>
        {
            RefreshConnectionList();

            return Task.FromResult(0);
        }).Forget();
    }

    private void CredentialsUpdateHandler(object sender, ServerConnectionUpdatedEventArgs e)
    {
        threadHandling.RunOnBackgroundThread(() =>
        {
            UpdateCredentials(e.ServerConnection.Id);

            return Task.FromResult(0);
        }).Forget();
    }

    public void Dispose()
    {
        serverConnectionsRepository.ConnectionChanged -= ConnectionUpdateHandler;
        serverConnectionsRepository.CredentialsChanged -= CredentialsUpdateHandler;
        asyncLock.Dispose();
    }
}
