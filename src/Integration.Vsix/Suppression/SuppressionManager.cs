/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Linq;
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.Vsix.Suppression
{
    internal sealed class SuppressionManager : IDisposable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly ISonarQubeService sonarQubeService;

        private DelegateInjector delegateInjector;
        private LiveIssueFactory liveIssueFactory;
        private ISonarQubeIssuesProvider sonarqubeIssueProvider;

        public SuppressionManager(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            this.activeSolutionBoundTracker = serviceProvider.GetMefService<IActiveSolutionBoundTracker>();
            this.activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;

            this.sonarQubeService = serviceProvider.GetMefService<ISonarQubeService>();

            RefreshSuppresionHandling();
        }

        private void RefreshSuppresionHandling()
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

        private void SetupSuppressionHandling()
        {
            liveIssueFactory = new LiveIssueFactory(serviceProvider);
            delegateInjector = new DelegateInjector(ShouldIssueBeReported, serviceProvider);
            sonarqubeIssueProvider = new SonarQubeIssuesProvider(sonarQubeService, activeSolutionBoundTracker);
        }

        private void CleanupSuppressionHandling()
        {
            delegateInjector?.Dispose();
            delegateInjector = null;
            liveIssueFactory = null;
            (sonarqubeIssueProvider as IDisposable)?.Dispose();
            sonarqubeIssueProvider = null;
        }

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            RefreshSuppresionHandling();
        }

        private bool ShouldIssueBeReported(Diagnostic diagnostic)
        {
            // This method is called for every analyzer issue that is raised so it should be fast.
            if (!diagnostic.Location.IsInSource) { return true; }
            if (activeSolutionBoundTracker == null || !activeSolutionBoundTracker.IsActiveSolutionBound) { return true; }

            LiveIssue liveIssue = liveIssueFactory.Create(diagnostic);
            if (liveIssue == null)
            {
                return true; // Unable to get the data required to map a Roslyn issue to a SonarQube issue
            }

            // Issues match if:
            // 1. Same component, same file, same error code, same line hash        // tolerant to line number changing
            // 2. Same component, same file, same error code, same line             // tolerant to code on the line changing e.g. var rename

            // TODO: ?need to make file path relative to the project file path
            // As a minimum, the project, file and rule id must match
            var issuesInFile = sonarqubeIssueProvider.GetSuppressedIssues(liveIssue.ProjectGuid, liveIssue.IssueFilePath);

            if (issuesInFile == null)
            {
                return true;
            }

            // TODO: rule repository?
            issuesInFile = issuesInFile.Where(i => StringComparer.OrdinalIgnoreCase.Equals(liveIssue.Diagnostic.Id, i.RuleId));

            bool matchFound = issuesInFile.Any(i =>
                    liveIssue.StartLine == i.Line ||
                    StringComparer.Ordinal.Equals(liveIssue.LineHash, i.Hash));
            return !matchFound;
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
