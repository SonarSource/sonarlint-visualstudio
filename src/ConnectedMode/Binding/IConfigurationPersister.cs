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
using System.IO;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    public interface IConfigurationPersister
    {
        LegacyBindingConfiguration Persist(BoundSonarQubeProject project);
    }

    [Export(typeof(IConfigurationPersister))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ConfigurationPersister : IConfigurationPersister
    {
        private readonly IUnintrusiveBindingPathProvider configFilePathProvider;
        private readonly ISolutionBindingRepository solutionBindingRepository;

        [ImportingConstructor]
        public ConfigurationPersister(
            IUnintrusiveBindingPathProvider configFilePathProvider,
            ISolutionBindingRepository solutionBindingRepository)
        {
            this.configFilePathProvider = configFilePathProvider;

            this.solutionBindingRepository = solutionBindingRepository;
        }

        public LegacyBindingConfiguration Persist(BoundSonarQubeProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var configFilePath = configFilePathProvider.GetCurrentBindingPath();

            var success = configFilePath != null &&
                          solutionBindingRepository.Write(configFilePath, project);

            // The binding directory is the folder containing the binding config file
            var bindingConfigDirectory = Path.GetDirectoryName(configFilePath);
            return success ?
                LegacyBindingConfiguration.CreateBoundConfiguration(project, SonarLintMode.Connected, bindingConfigDirectory) : null;
        }
    }
}
