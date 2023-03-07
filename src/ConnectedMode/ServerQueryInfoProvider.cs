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

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode
{
    /// <summary>
    /// Returns the information necessary to make various queries to the Sonar server
    /// </summary>
    /// <remarks>Facade to simplify classes that need to make branch-aware calls.
    /// The projectKey and server branch are provided by different interfaces. This interface
    /// simplifies getting the information.</remarks>
    internal interface IServerQueryInfoProvider
    {
        /// <summary>
        /// Returns the projectKey and branch name for the current bound solution, or
        /// (null, null) if the solution is not bound
        /// </summary>
        Task<(string projectKey, string branchName)> GetProjectKeyAndBranchAsync(CancellationToken token);
    }

    [Export(typeof(IServerQueryInfoProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ServerQueryInfoProvider : IServerQueryInfoProvider
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly IStatefulServerBranchProvider serverBranchProvider;

        [ImportingConstructor]
        public ServerQueryInfoProvider(IConfigurationProvider configurationProvider, IStatefulServerBranchProvider serverBranchProvider)
        {
            this.configurationProvider = configurationProvider;
            this.serverBranchProvider = serverBranchProvider;
        }

        public async Task<(string projectKey, string branchName)> GetProjectKeyAndBranchAsync(CancellationToken token)
        {
            var config = configurationProvider.GetConfiguration();
            if (config.Mode == SonarLintMode.Standalone)
            {
                return (null, null);
            }

            var branchName = await serverBranchProvider.GetServerBranchNameAsync(token);
            return (config.Project.ProjectKey, branchName);
        }
    }
}
