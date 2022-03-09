/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.InProcess
{
    /// <summary>
    /// Responsible for listening to <see cref="ISuppressedIssuesMonitor.SuppressionsUpdateRequested"/> and calling
    /// <see cref="ISuppressedIssuesFileStorage.Update"/> with the new suppressions.
    /// </summary>
    public interface ISuppressedIssuesFileSynchronizer : IDisposable
    {
        void UpdateFileStorage();
    }

    [Export(typeof(ISuppressedIssuesFileSynchronizer))]
    internal sealed class SuppressedIssuesFileSynchronizer : ISuppressedIssuesFileSynchronizer
    {
        private readonly IThreadHandling threadHandling;
        private readonly ISuppressedIssuesMonitor suppressedIssuesMonitor;
        private readonly ISonarQubeIssuesProvider suppressedIssuesProvider;
        private readonly ISuppressedIssuesFileStorage suppressedIssuesFileStorage;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;

        [ImportingConstructor]
        public SuppressedIssuesFileSynchronizer(ISuppressedIssuesMonitor suppressedIssuesMonitor,
            ISonarQubeIssuesProvider suppressedIssuesProvider,
            ISuppressedIssuesFileStorage suppressedIssuesFileStorage,
            IActiveSolutionBoundTracker activeSolutionBoundTracker)
            : this(suppressedIssuesMonitor,
                suppressedIssuesProvider,
                suppressedIssuesFileStorage,
                activeSolutionBoundTracker,
                new ThreadHandling())
        {
        }

        internal SuppressedIssuesFileSynchronizer(ISuppressedIssuesMonitor suppressedIssuesMonitor,
            ISonarQubeIssuesProvider suppressedIssuesProvider,
            ISuppressedIssuesFileStorage suppressedIssuesFileStorage,
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            IThreadHandling threadHandling)
        {
            this.suppressedIssuesMonitor = suppressedIssuesMonitor;
            this.suppressedIssuesProvider = suppressedIssuesProvider;
            this.suppressedIssuesFileStorage = suppressedIssuesFileStorage;
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.threadHandling = threadHandling;

            suppressedIssuesMonitor.SuppressionsUpdateRequested += SuppressedIssuesMonitor_SuppressionsUpdateRequested;
        }

        private void SuppressedIssuesMonitor_SuppressionsUpdateRequested(object sender, EventArgs e)
        {
            UpdateFileStorage();
        }

        public void UpdateFileStorage()
        {
            threadHandling.Run(() =>
            {
                var sonarProjectKey = activeSolutionBoundTracker.CurrentConfiguration.Project?.ProjectKey;

                if (!string.IsNullOrEmpty(sonarProjectKey))
                {
                    var allSuppressedIssues = suppressedIssuesProvider.GetAllSuppressedIssues();
                    suppressedIssuesFileStorage.Update(sonarProjectKey, allSuppressedIssues);
                }

                return Task.FromResult(true);
            });
        }

        public void Dispose()
        {
            suppressedIssuesMonitor.SuppressionsUpdateRequested -= SuppressedIssuesMonitor_SuppressionsUpdateRequested;
        }
    }
}
