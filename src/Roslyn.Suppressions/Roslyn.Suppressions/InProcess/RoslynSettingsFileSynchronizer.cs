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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.InProcess
{
    /// <summary>
    /// Responsible for listening to <see cref="ISuppressedIssuesMonitor.SuppressionsUpdateRequested"/> and calling
    /// <see cref="IRoslynSettingsFileStorage.Update"/> with the new suppressions.
    /// </summary>
    public interface IRoslynSettingsFileSynchronizer : IDisposable
    {
        Task UpdateFileStorageAsync();
    }

    [Export(typeof(IRoslynSettingsFileSynchronizer))]
    internal sealed class RoslynSettingsFileSynchronizer : IRoslynSettingsFileSynchronizer
    {
        private readonly IThreadHandling threadHandling;
        private readonly ISuppressedIssuesMonitor suppressedIssuesMonitor;
        private readonly ISonarQubeIssuesProvider suppressedIssuesProvider;
        private readonly IRoslynSettingsFileStorage suppressedIssuesFileStorage;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly ILogger logger;

        [ImportingConstructor]
        public RoslynSettingsFileSynchronizer(ISuppressedIssuesMonitor suppressedIssuesMonitor,
            ISonarQubeIssuesProvider suppressedIssuesProvider,
            IRoslynSettingsFileStorage suppressedIssuesFileStorage,
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            ILogger logger)
            : this(suppressedIssuesMonitor,
                suppressedIssuesProvider,
                suppressedIssuesFileStorage,
                activeSolutionBoundTracker,
                logger,
                new ThreadHandling())
        {
        }

        internal RoslynSettingsFileSynchronizer(ISuppressedIssuesMonitor suppressedIssuesMonitor,
            ISonarQubeIssuesProvider suppressedIssuesProvider,
            IRoslynSettingsFileStorage suppressedIssuesFileStorage,
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            ILogger logger,
            IThreadHandling threadHandling)
        {
            this.suppressedIssuesMonitor = suppressedIssuesMonitor;
            this.suppressedIssuesProvider = suppressedIssuesProvider;
            this.suppressedIssuesFileStorage = suppressedIssuesFileStorage;
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.logger = logger;
            this.threadHandling = threadHandling;

            suppressedIssuesMonitor.SuppressionsUpdateRequested += SuppressedIssuesMonitor_SuppressionsUpdateRequested;
        }

        private void SuppressedIssuesMonitor_SuppressionsUpdateRequested(object sender, EventArgs e)
        {
            // Called on the UI thread, so unhandled exceptions will crash VS.
            // Note: we don't expect any exceptions to be thrown, since the called method
            // does all of its work on a background thread.
            try
            {
                UpdateFileStorageAsync().Forget();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Squash non-critical exceptions
                logger.LogDebugExtended(ex.ToString());
            }
        }

        /// <summary>
        /// Updates the Roslyn suppressed issues file if in connected mode
        /// </summary>
        /// <remarks>The method will switch to a background if required, and will *not*
        /// return to the UI thread on completion.</remarks>
        public async Task UpdateFileStorageAsync()
        {
            logger.LogDebugExtended("Start");
            await threadHandling.SwitchToBackgroundThread();
            logger.LogDebugExtended("On background thread");

            var sonarProjectKey = activeSolutionBoundTracker.CurrentConfiguration.Project?.ProjectKey;

            if (!string.IsNullOrEmpty(sonarProjectKey))
            {
                var allSuppressedIssues = await suppressedIssuesProvider.GetAllSuppressedIssuesAsync();
                var settings = new RoslynSettings
                {
                    SonarProjectKey = sonarProjectKey,
                    Suppressions = allSuppressedIssues,
                };
                suppressedIssuesFileStorage.Update(settings);
            }

            logger.LogDebugExtended("End");
        }

        public void Dispose()
        {
            suppressedIssuesMonitor.SuppressionsUpdateRequested -= SuppressedIssuesMonitor_SuppressionsUpdateRequested;
        }
    }
}
