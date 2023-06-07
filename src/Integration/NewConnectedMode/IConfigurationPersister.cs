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
using System.IO;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.UnintrusiveBinding;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    public interface IConfigurationPersister : ILocalService
    {
        BindingConfiguration Persist(BoundSonarQubeProject project);
    }

    internal class ConfigurationPersister : IConfigurationPersister
    {
        private readonly IUnintrusiveBindingPathProvider configFilePathProvider;
        private readonly ISolutionBindingDataWriter solutionBindingDataWriter;

        public ConfigurationPersister(
            IUnintrusiveBindingPathProvider configFilePathProvider,
            ISolutionBindingDataWriter solutionBindingDataWriter)
        {
            this.configFilePathProvider = configFilePathProvider ??
                                             throw new ArgumentNullException(nameof(ConfigurationPersister.configFilePathProvider));

            this.solutionBindingDataWriter = solutionBindingDataWriter ??
                                             throw new ArgumentNullException(nameof(solutionBindingDataWriter));
        }

        public BindingConfiguration Persist(BoundSonarQubeProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var configFilePath = configFilePathProvider.Get();

            var success = configFilePath != null &&
                          solutionBindingDataWriter.Write(configFilePath, project);

            // The binding directory is the folder containing the binding config file
            var bindingConfigDirectory = Path.GetDirectoryName(configFilePath);
            return success ?
                BindingConfiguration.CreateBoundConfiguration(project, SonarLintMode.Connected, bindingConfigDirectory) : null;
        }
    }
}
