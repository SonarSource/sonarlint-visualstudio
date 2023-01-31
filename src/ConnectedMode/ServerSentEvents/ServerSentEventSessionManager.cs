﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode.ServerSentEvents
{
    internal interface IServerSentEventSessionManager : IDisposable
    {
    }

    [Export(typeof(IServerSentEventSessionManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ServerSentEventSessionManager : IServerSentEventSessionManager
    {
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly ISonarQubeService sonarQubeClient;
        private readonly IServerSentEventPump eventPump;
        private readonly IThreadHandling threadHandling;
        private bool disposed;
        private CancellationTokenSource sessionTokenSource;

        [ImportingConstructor]
        public ServerSentEventSessionManager(
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            ISonarQubeService sonarQubeClient,
            IServerSentEventPump eventPump,
            IThreadHandling threadHandling)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.sonarQubeClient = sonarQubeClient;
            this.eventPump = eventPump;
            this.threadHandling = threadHandling;
            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionChanged;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionChanged;
            sessionTokenSource?.Cancel();
            eventPump.Dispose();
            disposed = true;
        }

        private void OnSolutionChanged(object sender, ActiveSolutionBindingEventArgs activeSolutionBindingEventArgs)
        {
            var bindingConfiguration = activeSolutionBindingEventArgs.Configuration;
            var isInConnectedMode = !bindingConfiguration.Equals(BindingConfiguration.Standalone);

            EndSession();

            if (!isInConnectedMode)
            {
                return;
            }

            InitializeSessionAsync(bindingConfiguration.Project.ProjectKey).Forget();
        }

        private async Task InitializeSessionAsync(string projectKey)
        {
            await threadHandling.SwitchToBackgroundThread();

            sessionTokenSource = new CancellationTokenSource();
            var serverSentEventsSession =
                await sonarQubeClient.CreateServerSentEventsSession(projectKey,
                    sessionTokenSource.Token); //todo what kind of errors it throws? Sync with Rita

            if (serverSentEventsSession == null)
            {
                return;
            }

            await eventPump.PumpAllAsync(serverSentEventsSession);
        }

        private void EndSession()
        {
            sessionTokenSource?.Cancel();
            sessionTokenSource = null;
        }
    }
}
