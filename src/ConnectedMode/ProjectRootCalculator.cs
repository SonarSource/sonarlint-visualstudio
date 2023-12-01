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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Helpers;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode
{
    internal interface IProjectRootCalculator
    {
        Task<string> CalculateBasedOnLocalPathAsync(string localPath, CancellationToken token);
    }

    [Export(typeof(IProjectRootCalculator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ProjectRootCalculator : IProjectRootCalculator
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IStatefulServerBranchProvider branchProvider;

        [ImportingConstructor]
        public ProjectRootCalculator(ISonarQubeService sonarQubeService, IActiveSolutionBoundTracker activeSolutionBoundTracker, IStatefulServerBranchProvider branchProvider)
        {
            this.sonarQubeService = sonarQubeService;
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.branchProvider = branchProvider;
        }

        public async Task<string> CalculateBasedOnLocalPathAsync(string localPath, CancellationToken token)
        {
            var bindingConfiguration = activeSolutionBoundTracker.CurrentConfiguration;

            if (bindingConfiguration.Mode == SonarLintMode.Standalone)
            {
                return null;
            }

            return PathHelper.CalculateServerRoot(localPath,
                await sonarQubeService.SearchFilesByNameAsync(bindingConfiguration.Project.ProjectKey,
                    await branchProvider.GetServerBranchNameAsync(token),
                    Path.GetFileName(localPath),
                    token));
        }
    }
}
