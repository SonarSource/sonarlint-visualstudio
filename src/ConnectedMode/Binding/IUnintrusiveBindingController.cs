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
using System.ComponentModel.Composition;
using System.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    internal interface IUnintrusiveBindingController
    {
        Task BindAsync(BoundSonarQubeProject project, IProgress<FixedStepsProgress> progress, CancellationToken token);
        Task BindAsync(BoundServerProject project, IProgress<FixedStepsProgress> progress, CancellationToken token);
    }

    [Export(typeof(IUnintrusiveBindingController))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class UnintrusiveBindingController : IUnintrusiveBindingController
    {
        private readonly IBindingProcessFactory bindingProcessFactory;
        private readonly IServerConnectionsRepository serverConnectionsRepository;
        private readonly ISolutionInfoProvider solutionInfoProvider;

        [ImportingConstructor]
        public UnintrusiveBindingController(IBindingProcessFactory bindingProcessFactory, IServerConnectionsRepository serverConnectionsRepository, ISolutionInfoProvider solutionInfoProvider)
        {
            this.bindingProcessFactory = bindingProcessFactory;
            this.serverConnectionsRepository = serverConnectionsRepository;
            this.solutionInfoProvider = solutionInfoProvider;
        }

        public async Task BindAsync(BoundServerProject project, IProgress<FixedStepsProgress> progress, CancellationToken token)
        {
            var bindingProcess = CreateBindingProcess(project);
            await bindingProcess.DownloadQualityProfileAsync(progress, token);
            await bindingProcess.SaveServerExclusionsAsync(token);
        }

        public async Task BindAsync(BoundSonarQubeProject project, IProgress<FixedStepsProgress> progress, CancellationToken token)
        {
            var proposedConnection = ServerConnection.FromBoundSonarQubeProject(project);

            if (proposedConnection is null)
            {
                throw new InvalidOperationException(BindingStrings.UnintrusiveController_InvalidConnection);
            }
            
            if (!serverConnectionsRepository.TryGet(proposedConnection.Id, out var connection))
            {
                if (!serverConnectionsRepository.TryAdd(proposedConnection))
                {
                    throw new InvalidOperationException(BindingStrings.UnintrusiveController_CantAddConnection);
                }

                connection = proposedConnection;
            }

            await BindAsync(BoundServerProject.FromBoundSonarQubeProject(project, await solutionInfoProvider.GetSolutionNameAsync(), connection), progress, token);
        }

        private IBindingProcess CreateBindingProcess(BoundServerProject project)
        {
            var bindingProcess = bindingProcessFactory.Create(new BindCommandArgs(project));

            return bindingProcess;
        }
    }
}
