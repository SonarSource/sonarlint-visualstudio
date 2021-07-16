/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

namespace SonarLint.VisualStudio.Integration
{
    internal class UnboundProjectFinder : IUnboundProjectFinder
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IProjectBinderFactory projectBinderFactory;
        private readonly ILogger logger;

        public UnboundProjectFinder(IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, logger, new ProjectBinderFactory(serviceProvider, logger))
        {
        }

        internal UnboundProjectFinder(IServiceProvider serviceProvider, ILogger logger, IProjectBinderFactory projectBinderFactory)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.projectBinderFactory = projectBinderFactory ?? throw new ArgumentNullException(nameof(projectBinderFactory));
        }

        public IEnumerable<Project> GetUnboundProjects()
        {
            logger.LogDebug($"[Binding check] Checking for unbound projects...");

            var configProvider = serviceProvider.GetService<IConfigurationProviderService>();
            configProvider.AssertLocalServiceIsNotNull();

            // Only applicable in connected mode (legacy or new)
            var bindingConfig = configProvider.GetConfiguration();
            logger.LogDebug($"[Binding check] Binding mode: {bindingConfig.Mode}");

            var unbound = bindingConfig.Mode.IsInAConnectedMode() ? GetUnboundProjects(bindingConfig) : Array.Empty<Project>();
            logger.LogDebug($"[Binding check] Number of unbound projects: {unbound.Length}");
            return unbound;
        }

        private Project[] GetUnboundProjects(BindingConfiguration binding)
        {
            Debug.Assert(binding.Mode.IsInAConnectedMode());
            Debug.Assert(binding.Project != null);

            var projectSystem = serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();

            var filteredSolutionProjects = projectSystem.GetFilteredSolutionProjects().ToArray();
            logger.LogDebug($"[Binding check] Number of bindable projects: {filteredSolutionProjects.Length}");

            return filteredSolutionProjects
                .Where(project =>
                {
                    var configProjectBinder = projectBinderFactory.Get(project);

                    logger.LogDebug($"[Binding check] Checking binding for project '{project.Name}'. Binder type: {configProjectBinder.GetType().Name}");
                    var required = configProjectBinder.IsBindingRequired(binding, project);
                    logger.LogDebug($"[Binding check] Is binding required: {required} (project: {project.Name})");

                    return required;
                })
                .ToArray();
        }
    }
}
