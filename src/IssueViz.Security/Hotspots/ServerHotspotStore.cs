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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots
{
    internal interface IServerHotspotStore
    {
        Task UpdateAsync(string projectKey, string branch, CancellationToken token);

        Task<IList<SonarQubeHotspotSearch>> GetServerHotspotsAsync(string projectKey, string branch, CancellationToken token);

        event EventHandler<ServerHotspotStoreUpdatedEventArgs> ServerHotspotStoreUpdated;
    }

    [Export(typeof(IServerHotspotStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ServerHotspotStore : IServerHotspotStore
    {
        private readonly ISonarQubeService sonarQubeService;
        internal /*for testing*/ readonly static Dictionary<string, IList<SonarQubeHotspotSearch>> serverHotspots = new Dictionary<string, IList<SonarQubeHotspotSearch>>();

        public event EventHandler<ServerHotspotStoreUpdatedEventArgs> ServerHotspotStoreUpdated;

        [ImportingConstructor]
        public ServerHotspotStore(ISonarQubeService sonarQubeService)
        {
            this.sonarQubeService = sonarQubeService;
        }

        public async Task UpdateAsync(string projectKey, string branch, CancellationToken token)
        {
            var compositeKey = CreateCompositeKey(projectKey, branch);

            var serverResult = await sonarQubeService.SearchHotspotsAsync(projectKey, branch, token);

            if (serverHotspots.ContainsKey(compositeKey))
            {
                serverHotspots.Remove(compositeKey);
            }

            serverHotspots.Add(compositeKey, serverResult);
            RaiseServerHotspotStoreUpdated(projectKey, branch);
        }

        public async Task<IList<SonarQubeHotspotSearch>> GetServerHotspotsAsync(string projectKey, string branch, CancellationToken token)
        {
            var compositeKey = CreateCompositeKey(projectKey, branch);

            if (!serverHotspots.ContainsKey(compositeKey))
            {
                await UpdateAsync(projectKey, branch, token);
            }

            return serverHotspots[compositeKey];
        }

        internal /* for testing */ static string CreateCompositeKey(string projectKey, string branch)
        {
            return $"{projectKey}:{branch}";
        }

        private void RaiseServerHotspotStoreUpdated(string projectKey, string branchName)
        {
            ServerHotspotStoreUpdated.Invoke(this, new ServerHotspotStoreUpdatedEventArgs(projectKey, branchName));
        }
    }

    internal class ServerHotspotStoreUpdatedEventArgs : EventArgs
    {
        public ServerHotspotStoreUpdatedEventArgs(string projectKey, string branchName)
        {
            ProjectKey = projectKey;
            BranchName = branchName;
        }

        public string ProjectKey { get; }
        public string BranchName { get; }
    }
}
