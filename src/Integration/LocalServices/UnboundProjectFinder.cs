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
using System.Threading.Tasks;
using EnvDTE;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.ETW;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

// ---------
// Threading
// ---------
// Determining whether a project is bound or not requires heavy usage of methods/properties
// that must be called on the UI, so most of the processing must be on the UI thread.
// However, a solution could have hundreds of projects, so we can't tie up the UI thread
// for that long without triggering a perf gold bar. See 
//
// The approach taken is as follows:
// * GetUnboundProjects must be called on a background thread
// * it fetches the list of projects to check on the UI thread
// * it loops round each project on the background thread
// * inside the loop, each project is checked on the UI thread
//
// The class has ETW events that can be used to investigate threading and performance issues.

namespace SonarLint.VisualStudio.Integration
{
    internal class UnboundProjectFinder : IUnboundProjectFinder
    {
        private readonly IProjectBinderFactory projectBinderFactory;
        private readonly IConfigurationProviderService configProvider;
        private readonly IProjectSystemHelper projectSystem;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;

        public UnboundProjectFinder(IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, logger, new ProjectBinderFactory(serviceProvider, logger), new ThreadHandling())
        {
        }

        internal UnboundProjectFinder(IServiceProvider serviceProvider, ILogger logger,
            IProjectBinderFactory projectBinderFactory,IThreadHandling threadHandling)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.projectBinderFactory = projectBinderFactory ?? throw new ArgumentNullException(nameof(projectBinderFactory));
            this.threadHandling = threadHandling ?? throw new ArgumentNullException(nameof(threadHandling));

            configProvider = serviceProvider.GetService<IConfigurationProviderService>();
            projectSystem = serviceProvider.GetService<IProjectSystemHelper>();
        }

        public IEnumerable<Project> GetUnboundProjects()
        {
            threadHandling.ThrowIfOnUIThread();

            CodeMarkers.Instance.UnboundProjectFinderStart();

            logger.LogDebug($"[Binding check] Checking for unbound projects...");

            // Only applicable in connected mode (legacy or new)
            var bindingConfig = configProvider.GetConfiguration();
            logger.LogDebug($"[Binding check] Binding mode: {bindingConfig.Mode}");

            var unbound = bindingConfig.Mode.IsInAConnectedMode() ? GetUnboundProjects(bindingConfig) : Array.Empty<Project>();
            logger.LogDebug($"[Binding check] Number of unbound projects: {unbound.Length}");

            CodeMarkers.Instance.UnboundProjectFinderStop();
            return unbound;
        }

        private Project[] GetUnboundProjects(BindingConfiguration binding)
        {
            threadHandling.ThrowIfOnUIThread();

            Debug.Assert(binding.Mode.IsInAConnectedMode());
            Debug.Assert(binding.Project != null);

            // Using threadHandling.Run(...) here to avoid having to convert all of the upstream
            // calling methods to be async.
            var projects = threadHandling.Run(() => GetUnboundProjectsAsync(binding));

            threadHandling.ThrowIfOnUIThread();
            return projects;
        }

        private async Task<Project[]> GetUnboundProjectsAsync(BindingConfiguration binding)
        {
            threadHandling.ThrowIfOnUIThread();

            Debug.Assert(binding.Mode.IsInAConnectedMode());
            Debug.Assert(binding.Project != null);

            var filteredSolutionProjects = await GetFilteredSolutionProjectsAsync();

            threadHandling.ThrowIfOnUIThread();

            var unboundProjects = new List<Project>();
            foreach (var project in filteredSolutionProjects)
            {
                CodeMarkers.Instance.UnboundProjectFinderBeforeIsBindingRequired();
                var isUnbound = await IsBindingRequiredAsync(project, binding);

                if (isUnbound)
                {
                    unboundProjects.Add(project);
                }
            }

            return unboundProjects.ToArray();
        }

        private async Task<Project[]> GetFilteredSolutionProjectsAsync()
        {
            threadHandling.ThrowIfOnUIThread();
            Project[] filteredSolutionProjects = null;

            await threadHandling.RunOnUIThread(() =>
            {
                filteredSolutionProjects = projectSystem.GetFilteredSolutionProjects().ToArray();
                logger.LogDebug($"[Binding check] Number of bindable projects: {filteredSolutionProjects.Length}");
            });

            return filteredSolutionProjects;
        }

        private async Task<bool> IsBindingRequiredAsync(Project project, BindingConfiguration binding)
        {
            bool isBindingRequired = false;
            await threadHandling.RunOnUIThread(() => isBindingRequired = IsBindingRequired(project, binding));
            return isBindingRequired;
        }

        private bool IsBindingRequired(Project project, BindingConfiguration binding)
        {
            threadHandling.ThrowIfNotOnUIThread();

            var configProjectBinder = projectBinderFactory.Get(project);

            var projectName = project.Name;
            ETW.CodeMarkers.Instance.CheckProjectBindingStart(projectName);

            logger.LogDebug($"[Binding check] Checking binding for project '{projectName}'. Binder type: {configProjectBinder.GetType().Name}");
            var required = configProjectBinder.IsBindingRequired(binding, project);
            logger.LogDebug($"[Binding check] Is binding required: {required} (project: {projectName})");

            ETW.CodeMarkers.Instance.CheckProjectBindingStop();

            return required;
        }
    }
}
