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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Suppression
{
    [Export(typeof(ISonarQubeIssuesProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SuppressedIssuesProvider : ISonarQubeIssuesProvider
    {
        public delegate ISonarQubeIssuesProvider CreateProviderFunc(BindingConfiguration bindingConfiguration);

        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly CreateProviderFunc createProviderFunc;

        private ISonarQubeIssuesProvider instance;
        private bool disposed;

        [ImportingConstructor]
        public SuppressedIssuesProvider(IActiveSolutionBoundTracker activeSolutionBoundTracker,
            ISonarQubeService sonarQubeService,
            ILogger logger)
            : this(activeSolutionBoundTracker, GetCreateProviderFunc(sonarQubeService, logger))
        {
        }

        private static CreateProviderFunc GetCreateProviderFunc(ISonarQubeService sonarQubeService, ILogger logger)
        {
            return bindingConfiguration => new SonarQubeIssuesProvider(
                sonarQubeService,
                bindingConfiguration.Project.ProjectKey,
                new TimerFactory(),
                logger);
        }

        internal SuppressedIssuesProvider(IActiveSolutionBoundTracker activeSolutionBoundTracker, 
            CreateProviderFunc createProviderFunc)
        {
            this.createProviderFunc = createProviderFunc ??
                                          throw new ArgumentNullException(nameof(createProviderFunc));

            this.activeSolutionBoundTracker = activeSolutionBoundTracker ??
                                              throw new ArgumentNullException(nameof(activeSolutionBoundTracker));

            this.activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;
            this.activeSolutionBoundTracker.SolutionBindingUpdated += OnSolutionBindingUpdated;
        }

        public IEnumerable<SonarQubeIssue> GetSuppressedIssues(string projectGuid, string filePath)
        {
            return instance?.GetSuppressedIssues(projectGuid, filePath) ?? new List<SonarQubeIssue>();
        }

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            Refresh(e.Configuration);
        }

        private void OnSolutionBindingUpdated(object sender, EventArgs e)
        {
            Refresh(activeSolutionBoundTracker.CurrentConfiguration);
        }

        private void Refresh(BindingConfiguration configuration)
        {
            CleanupResources();
            
            if (configuration.Mode != SonarLintMode.Standalone)
            {
                instance = createProviderFunc(configuration);
            }
        }

        private void CleanupResources()
        {
            instance?.Dispose();
            instance = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                CleanupResources();
                activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
                activeSolutionBoundTracker.SolutionBindingUpdated -= OnSolutionBindingUpdated;
                disposed = true;
            }
        }
    }
}
