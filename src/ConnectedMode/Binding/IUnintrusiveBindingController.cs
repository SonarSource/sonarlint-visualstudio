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
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    public interface IBindingController
    {
        Task BindAsync(BoundServerProject project, CancellationToken cancellationToken);
    }
    
    internal interface IUnintrusiveBindingController
    {
        Task BindAsync(BoundServerProject project, IProgress<FixedStepsProgress> progress, CancellationToken token);
    }

    [Export(typeof(IBindingController))]
    [Export(typeof(IUnintrusiveBindingController))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class UnintrusiveBindingController : IUnintrusiveBindingController, IBindingController
    {
        private readonly IBindingProcessFactory bindingProcessFactory;
        private readonly ISonarQubeService sonarQubeService;
        private readonly IActiveSolutionChangedHandler activeSolutionChangedHandler;

        [ImportingConstructor]
        public UnintrusiveBindingController(IBindingProcessFactory bindingProcessFactory, ISonarQubeService sonarQubeService, IActiveSolutionChangedHandler activeSolutionChangedHandler)
        {
            this.bindingProcessFactory = bindingProcessFactory;
            this.sonarQubeService = sonarQubeService;
            this.activeSolutionChangedHandler = activeSolutionChangedHandler;
        }

        public async Task BindAsync(BoundServerProject project, CancellationToken cancellationToken)
        {
            var connectionInformation = project.ServerConnection.Credentials.CreateConnectionInformation(project.ServerConnection.ServerUri);
            await sonarQubeService.ConnectAsync(connectionInformation, cancellationToken);
            await BindAsync(project, null, cancellationToken);
            activeSolutionChangedHandler.HandleBindingChange(false);
        }

        public async Task BindAsync(BoundServerProject project, IProgress<FixedStepsProgress> progress, CancellationToken token)
        {
            var bindingProcess = CreateBindingProcess(project);
            await bindingProcess.DownloadQualityProfileAsync(progress, token);
            await bindingProcess.SaveServerExclusionsAsync(token);
        }

        private IBindingProcess CreateBindingProcess(BoundServerProject project)
        {
            var bindingProcess = bindingProcessFactory.Create(new BindCommandArgs(project));

            return bindingProcess;
        }
    }
}
