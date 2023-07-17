/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Helpers;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode.Hotspots
{
    /// <summary>
    /// Fetches Hotspots from the server and updates the server hotspot store
    /// </summary>
    public interface IServerHotspotStoreUpdater : IDisposable
    {
        /// <summary>
        /// Fetches all available hotspots from the server and updates the <see cref="IServerHotspotStore"/>
        /// </summary>
        Task UpdateAllServerHotspotsAsync();
    }

    [Export(typeof(IServerHotspotStoreUpdater))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ServerHotspotStoreUpdater : IServerHotspotStoreUpdater
    {
        [ImportingConstructor]
        public ServerHotspotStoreUpdater(ISonarQubeService sonarQube,
            IServerHotspotStore serverHotspotStore,
            IServerQueryInfoProvider serverQueryInfoProvider,
            ICancellableActionRunner actionRunner,
            IThreadHandling threadHandling,
            ILogger logger)
        {
            this.sonarQube = sonarQube;
            this.serverHotspotStore = serverHotspotStore;
            this.serverQueryInfoProvider = serverQueryInfoProvider;
            this.actionRunner = actionRunner;
            this.threadHandling = threadHandling;
            this.logger = logger;
        }

        private readonly ISonarQubeService sonarQube;
        private readonly IServerHotspotStore serverHotspotStore;
        private readonly IServerQueryInfoProvider serverQueryInfoProvider;
        private readonly ICancellableActionRunner actionRunner;
        private readonly IThreadHandling threadHandling;
        private readonly ILogger logger;

        public async Task UpdateAllServerHotspotsAsync()
        {
            await threadHandling.SwitchToBackgroundThread();
            await actionRunner.RunAsync(async token =>
            {
                try
                {
                    logger.WriteLine(Resources.Hotspots_Fetch_AllHotspots);
                    
                    var (projectKey, branchName) = await serverQueryInfoProvider.GetProjectKeyAndBranchAsync(token);
                    
                    if (projectKey == null || branchName == null)
                    {
                        return;
                    }
                    
                    token.ThrowIfCancellationRequested();
                    
                    serverHotspotStore.Refresh((await sonarQube.SearchHotspotsAsync(projectKey, branchName, token))
                        .Select(x => x.ToSonarQubeHotspot())
                        .ToArray());
                    
                    logger.WriteLine(Resources.Hotspots_Fetch_AllHotspots_Finished);
                }
                catch (OperationCanceledException)
                {
                    logger.WriteLine(Resources.Hotspots_FetchOperationCancelled);
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger.LogVerbose(Resources.Hotspots_FetchError_Verbose, ex);
                    logger.WriteLine(Resources.Hotpots_FetchError_Short, ex.Message);
                }
            });
        }

        public void Dispose()
        {
            actionRunner?.Dispose();
        }
    }
}
