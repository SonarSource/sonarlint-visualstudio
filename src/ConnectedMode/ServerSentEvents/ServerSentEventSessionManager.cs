/*
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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ServerSentEvents.Issues;
using SonarLint.VisualStudio.Core.ServerSentEvents.TaintVulnerabilities;
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
        private readonly IIssueChangedServerEventSourcePublisher issueChangedServerEventSourcePublisher;
        private readonly ISonarQubeService sonarQubeClient;
        private readonly IServerSentEventPump eventPump;
        private readonly ITaintServerEventSourcePublisher taintServerEventSourcePublisher;
        private readonly IThreadHandling threadHandling;
        private bool disposed;
        private CancellationTokenSource sessionTokenSource;

        [ImportingConstructor]
        public ServerSentEventSessionManager(
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            ISonarQubeService sonarQubeClient,
            IServerSentEventPump eventPump,
            IIssueChangedServerEventSourcePublisher issueChangedServerEventSourcePublisher,
            ITaintServerEventSourcePublisher taintServerEventSourcePublisher,
            IThreadHandling threadHandling)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.sonarQubeClient = sonarQubeClient;
            this.eventPump = eventPump;
            this.issueChangedServerEventSourcePublisher = issueChangedServerEventSourcePublisher;
            this.taintServerEventSourcePublisher = taintServerEventSourcePublisher;
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
            issueChangedServerEventSourcePublisher?.Dispose();
            taintServerEventSourcePublisher?.Dispose();
            disposed = true;
        }

        internal /*for testing*/ void OnSolutionChanged(object sender, ActiveSolutionBindingEventArgs activeSolutionBindingEventArgs)
        {
            var bindingConfiguration = activeSolutionBindingEventArgs.Configuration;
            var isOpen = !bindingConfiguration.Equals(BindingConfiguration.Standalone);

            if (isOpen)
            {
                var projectKey = bindingConfiguration.Project.ProjectKey;
                Connect(projectKey);
            }
            else
            {
                Disconnect();
            }
        }

        private void Connect(string projectKey)
        {
            sessionTokenSource = new CancellationTokenSource();

            // fire and forget
            threadHandling.RunOnBackgroundThread(async () =>
            {
                var serverSentEventsSession =
                    await sonarQubeClient.CreateServerSentEventsSession(projectKey,
                        sessionTokenSource.Token); //todo what kind of errors it throws? Sync with Rita

                if (serverSentEventsSession == null)
                {
                    return false;
                }

                await eventPump.PumpAllAsync(serverSentEventsSession, issueChangedServerEventSourcePublisher, taintServerEventSourcePublisher);
                return true;

            }).Forget();
        }

        private void Disconnect()
        {
            sessionTokenSource?.Cancel();
            sessionTokenSource = null;
        }
    }
}
