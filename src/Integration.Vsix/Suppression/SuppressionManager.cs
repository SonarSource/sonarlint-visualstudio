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

namespace SonarLint.VisualStudio.Integration.Vsix.Suppression
{
    internal sealed class SuppressionManager : IDisposable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;

        private DelegateInjector delegateInjector;
        private LiveIssueFactory liveIssueFactory;
        private ISonarQubeIssuesProvider sonarqubeIssueProvider;

        public SuppressionManager(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            activeSolutionBoundTracker = serviceProvider.GetMefService<IActiveSolutionBoundTracker>();
            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;

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
            delegateInjector = new DelegateInjector(ShouldIssueBeSuppressed, serviceProvider);
            sonarqubeIssueProvider = this.serviceProvider.GetMefService<ISonarQubeIssuesProvider>(); // Cannot do new because of IHost
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

        private bool ShouldIssueBeSuppressed(Diagnostic diagnostic)
        {
            // This method is called for every analyzer issue that is raised so it should be fast.
            if (!diagnostic.Location.IsInSource) { return false; }
            if (activeSolutionBoundTracker == null || !activeSolutionBoundTracker.IsActiveSolutionBound) { return false; }

            LiveIssue liveIssue = liveIssueFactory.Create(diagnostic);
            if (liveIssue == null)
            {
                return false; // Unable to get the data required to map a Roslyn issue to a SonarQube issue
            }

            // Issues match if:
            // 1. Same component, same file, same error code, same line hash        // tolerant to line number changing
            // 2. Same component, same file, same error code, same line             // tolarant to code on the line changing e.g. var rename

            // TODO: ?need to make file path relative to the project file path
            // As a minimum, the project, file and rule id must match
            var issuesInFile = sonarqubeIssueProvider.GetSuppressedIssues(liveIssue.ProjectGuid, liveIssue.IssueFilePath)
                    .Where(i => StringComparer.OrdinalIgnoreCase.Equals(liveIssue.Diagnostic.Id, i.RuleId)); // TODO: rule repository?

            return issuesInFile.Any(i =>
                    liveIssue.StartLine == i.Line ||
                    StringComparer.Ordinal.Equals(liveIssue.LineHash, i.Hash));
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
