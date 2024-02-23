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
/// 
/// </summary>
public interface IAliveConnectionTracker : IDisposable
{
    Task RefreshConnectionListAsync();
}

[Export(typeof(IAliveConnectionTracker))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class AliveConnectionTracker : IAliveConnectionTracker
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly ISolutionBindingRepository solutionBindingRepository;
    private readonly IConnectionIdHelper connectionIdHelper;
    private readonly IThreadHandling threadHandling;
    private readonly IAsyncLock asyncLock;
    
    [ImportingConstructor]
    public AliveConnectionTracker(ISLCoreServiceProvider serviceProvider,
        ISolutionBindingRepository solutionBindingRepository,
        IConnectionIdHelper connectionIdHelper,
        IAsyncLockFactory asyncLockFactory,
        IThreadHandling threadHandling)
    {
        this.serviceProvider = serviceProvider;
        this.solutionBindingRepository = solutionBindingRepository;
        this.threadHandling = threadHandling;
        asyncLock = asyncLockFactory.Create();
        this.connectionIdHelper = connectionIdHelper;

        solutionBindingRepository.BindingUpdated += BindingUpdateHandler;
    }

    public async Task RefreshConnectionListAsync()
    {
        threadHandling.ThrowIfOnUIThread();

        if (!serviceProvider.TryGetTransientService(out IConnectionConfigurationSLCoreService connectionConfigurationService))
        {
            throw new InvalidOperationException(Strings.ServiceProviderNotInitialized);
        }

        using (await asyncLock.AcquireAsync())
        {
            var serverConnections = GetUniqueConnections(solutionBindingRepository.List());

            await connectionConfigurationService.DidUpdateConnectionsAsync(new DidUpdateConnectionsParams(
                serverConnections.Values.OfType<SonarQubeConnectionConfigurationDto>().ToList(),
                serverConnections.Values.OfType<SonarCloudConnectionConfigurationDto>().ToList()));

            foreach (var connectionId in serverConnections.Keys)
            {
                // we don't manage connections as separate entities and we don't know when credentials actually change
                await connectionConfigurationService.DidChangeCredentialsAsync(
                    new DidChangeCredentialsParams(connectionId));
            }
        }
    }

    private void BindingUpdateHandler(object sender, EventArgs arg)
    {
        threadHandling.RunOnBackgroundThread(async () =>
        {
            await RefreshConnectionListAsync();

            return 0;
        }).Forget();
    }

    private Dictionary<string, ServerConnectionConfiguration> GetUniqueConnections(IEnumerable<BoundSonarQubeProject> bindings)
    {
        var connections = new Dictionary<string, ServerConnectionConfiguration>();

        foreach (var binding in bindings)
        {
            var serverUri = binding.ServerUri;
            var organization = binding.Organization?.Key;
            var connectionId = connectionIdHelper.GetConnectionIdFromUri(serverUri, organization);

            if (serverUri == ConnectionIdHelper.SonarCloudUri)
            {
                connections[connectionId] = new SonarCloudConnectionConfigurationDto(connectionId, true, organization);
            }
            else
            {
                connections[connectionId] = new SonarQubeConnectionConfigurationDto(connectionId, true, serverUri.ToString());
            }
        }

        return connections;
    }
    
    public void Dispose()
    {
        solutionBindingRepository.BindingUpdated -= BindingUpdateHandler;
        asyncLock.Dispose();
    }
}
