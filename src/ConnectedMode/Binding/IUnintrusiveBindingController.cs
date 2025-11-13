/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client;
using SonarQube.Client.Models;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    public interface IBindingController
    {
        Task BindAsync(BoundServerProject project, CancellationToken cancellationToken);

        bool Unbind(string localBindingKey);
    }

    [Export(typeof(IBindingController))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class UnintrusiveBindingController : IBindingController
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly IActiveSolutionChangedHandler activeSolutionChangedHandler;
        private readonly IConfigurationPersister configurationPersister;
        private readonly ISolutionBindingRepository solutionBindingRepository;

        [ImportingConstructor]
        public UnintrusiveBindingController(
            ISonarQubeService sonarQubeService,
            IActiveSolutionChangedHandler activeSolutionChangedHandler,
            ISolutionBindingRepository solutionBindingRepository,
            IConfigurationPersister configurationPersister)
        {
            this.sonarQubeService = sonarQubeService;
            this.activeSolutionChangedHandler = activeSolutionChangedHandler;
            this.configurationPersister = configurationPersister;
            this.solutionBindingRepository = solutionBindingRepository;
        }

        public async Task BindAsync(BoundServerProject project, CancellationToken cancellationToken)
        {
            var connectionInformation = new ConnectionInformation(project.ServerConnection.ServerUri, project.ServerConnection.Credentials);
            await sonarQubeService.ConnectAsync(connectionInformation, cancellationToken);
            configurationPersister.Persist(project);
            activeSolutionChangedHandler.HandleBindingChange();
        }

        public bool Unbind(string localBindingKey)
        {
            var bindingDeleted = solutionBindingRepository.DeleteBinding(localBindingKey);
            if (bindingDeleted)
            {
                sonarQubeService.Disconnect();
                activeSolutionChangedHandler.HandleBindingChange();
            }
            return bindingDeleted;
        }
    }
}
