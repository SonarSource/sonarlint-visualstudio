/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

namespace SonarLint.VisualStudio.Integration
{
    internal class UnboundProjectFinder : IUnboundProjectFinder
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IConfigProjectBinderFactory configProjectBinderFactory;

        public UnboundProjectFinder(IServiceProvider serviceProvider, IConfigProjectBinderFactory configProjectBinderFactory)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.configProjectBinderFactory = configProjectBinderFactory ?? throw new ArgumentNullException(nameof(configProjectBinderFactory));
        }

        public IEnumerable<Project> GetUnboundProjects()
        {
            var configProvider = serviceProvider.GetService<IConfigurationProvider>();
            configProvider.AssertLocalServiceIsNotNull();

            // Only applicable in connected mode (legacy or new)
            var bindingConfig = configProvider.GetConfiguration();

            return !bindingConfig.Mode.IsInAConnectedMode() ? Enumerable.Empty<Project>() : GetUnboundProjects(bindingConfig);
        }

        private IEnumerable<Project> GetUnboundProjects(BindingConfiguration binding)
        {
            Debug.Assert(binding.Mode.IsInAConnectedMode());
            Debug.Assert(binding.Project != null);

            var projectSystem = serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();

            return projectSystem.GetFilteredSolutionProjects()
                .Where(project =>
                {
                    var configProjectBinder = configProjectBinderFactory.Get(project);

                    return configProjectBinder == null || !configProjectBinder.IsBound(binding, project);
                })
                .ToArray();
        }
    }
}
