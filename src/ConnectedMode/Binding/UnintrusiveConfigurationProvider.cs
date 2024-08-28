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
using System.IO;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    [Export(typeof(IConfigurationProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class UnintrusiveConfigurationProvider : IConfigurationProvider
    {
        private readonly IUnintrusiveBindingPathProvider pathProvider;
        private readonly ISolutionBindingRepository solutionBindingRepository;

        [ImportingConstructor]
        public UnintrusiveConfigurationProvider(IUnintrusiveBindingPathProvider pathProvider, ISolutionBindingRepository solutionBindingRepository)
        {
            this.pathProvider = pathProvider;
            this.solutionBindingRepository = solutionBindingRepository;
        }

        public BindingConfiguration GetConfiguration()
        {
            var bindingConfiguration = TryGetBindingConfiguration(pathProvider.GetCurrentBindingPath());

            return bindingConfiguration ?? BindingConfiguration.Standalone;
        }

        private BindingConfiguration TryGetBindingConfiguration(string bindingPath)
        {
            if (bindingPath == null)
            {
                return null;
            }

            var boundProject = solutionBindingRepository.Read(bindingPath);

            if (boundProject == null)
            {
                return null;
            }

            var bindingConfigDirectory = Path.GetDirectoryName(bindingPath);

            return BindingConfiguration.CreateBoundConfiguration(BoundServerProject.FromBoundSonarQubeProject(boundProject),
                SonarLintMode.Connected,
                bindingConfigDirectory);
        }
    }
}
