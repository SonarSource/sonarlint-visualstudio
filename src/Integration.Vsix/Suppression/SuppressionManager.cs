/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.Vsix.Suppression
{
    internal sealed class SuppressionManager : IDisposable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly ILogger sonarLintOutput;

        private DelegateInjector delegateInjector;
        private ISonarQubeIssuesProvider sonarqubeIssueProvider;
        private SuppressionHandler suppressionHandler;

        public SuppressionManager(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;

            this.activeSolutionBoundTracker = serviceProvider.GetMefService<IActiveSolutionBoundTracker>();
            this.activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;

            this.sonarLintOutput = serviceProvider.GetMefService<ILogger>();

            RefreshSuppresionHandling();
        }

        private void RefreshSuppresionHandling()
        {
            // This method can be called on the UI thread so unhandled exceptions will crash VS
            try
            {
                if (activeSolutionBoundTracker.IsActiveSolutionBound)
                {
                    SetupSuppressionHandling();
                }
                else
                {
                    CleanupSuppressionHandling();
                }
            }
            catch(Exception ex)
            {
                this.sonarLintOutput.WriteLine($"Failed to refresh suppression handling: {ex.Message}");
            }
        }

        private void SetupSuppressionHandling()
        {
            var componentModel = serviceProvider.GetService<SComponentModel, IComponentModel>();
            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            var solution = this.serviceProvider.GetService<SVsSolution, IVsSolution>();

            var sonarQubeService = serviceProvider.GetMefService<ISonarQubeService>();

            LiveIssueFactory liveIssueFactory = new LiveIssueFactory(workspace, solution);
            delegateInjector = new DelegateInjector(ShouldIssueBeReported, sonarLintOutput);
            sonarqubeIssueProvider = new SonarQubeIssuesProvider(sonarQubeService, this.activeSolutionBoundTracker.ProjectKey,
                new TimerFactory());
            suppressionHandler = new SuppressionHandler(liveIssueFactory, sonarqubeIssueProvider);
        }

        private void CleanupSuppressionHandling()
        {
            delegateInjector?.Dispose();
            delegateInjector = null;
            (sonarqubeIssueProvider as IDisposable)?.Dispose();
            sonarqubeIssueProvider = null;
        }

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            RefreshSuppresionHandling();
        }

        private bool ShouldIssueBeReported(SyntaxTree syntaxTree, Diagnostic diagnostic)
        {
            // This method is called for every analyzer issue that is raised so it should be fast.

            if (activeSolutionBoundTracker == null ||
                !activeSolutionBoundTracker.IsActiveSolutionBound)
            {
                return true;
            }

            return suppressionHandler.ShouldIssueBeReported(syntaxTree, diagnostic);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    CleanupSuppressionHandling();
                    activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
