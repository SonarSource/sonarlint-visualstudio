﻿/*
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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.ServerSentEvents
{
    [Export(typeof(SSESessionManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SSESessionManager : IDisposable
    {
        private const int DelayTimeBetweenRetriesInMilliseconds = 1000;

        private readonly object syncRoot = new object();
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly ISSESessionFactory sseSessionFactory;
        private readonly ILogger logger;

        private ISSESession currentSession;

        private bool disposed;

        [ImportingConstructor]
        public SSESessionManager(IActiveSolutionBoundTracker activeSolutionBoundTracker,
            ISSESessionFactory sseSessionFactory,
            ILogger logger)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.sseSessionFactory = sseSessionFactory;
            this.logger = logger;

            activeSolutionBoundTracker.SolutionBindingChanged += SolutionBindingChanged;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            activeSolutionBoundTracker.SolutionBindingChanged -= SolutionBindingChanged;
            sseSessionFactory.Dispose();
            EndCurrentSession();
            disposed = true;
        }

        private void SolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs activeSolutionBindingEventArgs)
        {
            CreateSessionIfInConnectedMode(activeSolutionBindingEventArgs.Configuration);
        }

        /// <summary>
        /// Creates a new session if in connected mode. If no binding configuration is provided the
        /// ActiveSolutionBoundTracker.CurrentConfiguration will be used.
        /// </summary>
        public void CreateSessionIfInConnectedMode(BindingConfiguration bindingConfiguration = null)
        {
            if (bindingConfiguration == null) { bindingConfiguration = activeSolutionBoundTracker.CurrentConfiguration; }

            lock (syncRoot)
            {
                EndCurrentSession();

                var isInConnectedMode = !bindingConfiguration.Equals(BindingConfiguration.Standalone);

                if (!isInConnectedMode)
                {
                    logger.LogVerbose("[SSESessionManager] Not in connected mode");
                    return;
                }

                if (bindingConfiguration.Project.ServerConnection is ServerConnection.SonarCloud)
                {
                    logger.LogVerbose("[SSESessionManager] Not available for the current server connection");
                    return;
                }

                logger.LogVerbose("[SSESessionManager] In connected mode, creating session...");

                currentSession = sseSessionFactory.Create(bindingConfiguration.Project.ServerProjectKey, OnSessionFailedAsync);

                logger.LogVerbose("[SSESessionManager] Created session: {0}", currentSession.GetHashCode());

                currentSession.PumpAllAsync().Forget();

                logger.LogVerbose("[SSESessionManager] Session started");
            }
        }

        private async Task OnSessionFailedAsync(ISSESession failedSession)
        {
            logger.LogVerbose("[SSESessionManager] Session failed: " + failedSession.GetHashCode());

            await Task.Delay(DelayTimeBetweenRetriesInMilliseconds);
            CreateSessionIfInConnectedMode(activeSolutionBoundTracker.CurrentConfiguration);

            logger.LogVerbose("[SSESessionManager] Finished handling session failure");
        }

        private void EndCurrentSession()
        {
            logger.LogVerbose("[SSESessionManager] Ending current session...");

            lock (syncRoot)
            {
                logger.LogVerbose("[SSESessionManager] Disposing current session: {0}", currentSession?.GetHashCode());

                currentSession?.Dispose();
                currentSession = null;
            }
        }
    }
}
