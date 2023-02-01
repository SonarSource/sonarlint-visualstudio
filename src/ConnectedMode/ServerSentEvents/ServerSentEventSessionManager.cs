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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ServerSentEvents.Issues;
using SonarLint.VisualStudio.Core.ServerSentEvents.TaintVulnerabilities;
using SonarQube.Client;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

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
        private readonly ISSESessionFactory sseSessionFactory;

        private ISSESession currentSession;

        [ImportingConstructor]
        public ServerSentEventSessionManager(IActiveSolutionBoundTracker activeSolutionBoundTracker, ISSESessionFactory sseSessionFactory)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.sseSessionFactory = sseSessionFactory;

            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionChanged;
        }

        public void Dispose()
        {
            activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionChanged;
            currentSession?.Dispose();
        }

        private void OnSolutionChanged(object sender, ActiveSolutionBindingEventArgs activeSolutionBindingEventArgs)
        {
            var bindingConfiguration = activeSolutionBindingEventArgs.Configuration;
            var isInConnectedMode = !bindingConfiguration.Equals(BindingConfiguration.Standalone);

            currentSession?.Dispose();

            if (!isInConnectedMode)
            {
                return;
            }

            currentSession = sseSessionFactory.Create(bindingConfiguration.Project.ProjectKey);

            currentSession.PumpAllAsync().Forget();
        }
    }
    
    internal interface ISSESessionFactory
    {
        ISSESession Create(string projectKey);
    }

    internal interface ISSESession : IDisposable
    {
        Task PumpAllAsync();
    }

    [Export(typeof(ISSESessionFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SSESessionFactory : ISSESessionFactory
    {
        private readonly ISonarQubeService sonarQubeClient;
        private readonly IIssueChangedServerEventSourcePublisher issueChangedServerEventSourcePublisher;
        private readonly ITaintServerEventSourcePublisher taintServerEventSourcePublisher;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public SSESessionFactory(ISonarQubeService sonarQubeClient,
            IIssueChangedServerEventSourcePublisher issueChangedServerEventSourcePublisher, 
            ITaintServerEventSourcePublisher taintServerEventSourcePublisher,
            IThreadHandling threadHandling)
        {
            this.sonarQubeClient = sonarQubeClient;
            this.issueChangedServerEventSourcePublisher = issueChangedServerEventSourcePublisher;
            this.taintServerEventSourcePublisher = taintServerEventSourcePublisher;
            this.threadHandling = threadHandling;
        }

        public ISSESession Create(string projectKey)
        {
            var session = new SSESession(issueChangedServerEventSourcePublisher,
                taintServerEventSourcePublisher,
                projectKey,
                threadHandling,
                sonarQubeClient);
            
            return session;
        }

        private class SSESession : ISSESession
        {
            private readonly IIssueChangedServerEventSourcePublisher issueChangedServerEventSourcePublisher;
            private readonly ITaintServerEventSourcePublisher taintServerEventSourcePublisher;
            private readonly string projectKey;
            private readonly IThreadHandling threadHandling;
            private readonly ISonarQubeService sonarQubeService;
            private readonly CancellationTokenSource sessionTokenSource;

            public SSESession(IIssueChangedServerEventSourcePublisher issueChangedServerEventSourcePublisher, 
                ITaintServerEventSourcePublisher taintServerEventSourcePublisher,
                string projectKey,
                IThreadHandling threadHandling,
                ISonarQubeService sonarQubeService)
            {
                this.issueChangedServerEventSourcePublisher = issueChangedServerEventSourcePublisher;
                this.taintServerEventSourcePublisher = taintServerEventSourcePublisher;
                this.projectKey = projectKey;
                this.threadHandling = threadHandling;
                this.sonarQubeService = sonarQubeService;
                this.sessionTokenSource = new CancellationTokenSource();
            }

            public async Task PumpAllAsync()
            {
                await threadHandling.SwitchToBackgroundThread();

                // todo: rename to ISSEStreamReader
                var sseStreamReader =
                    await sonarQubeService.CreateServerSentEventsSession(projectKey, sessionTokenSource.Token); //todo what kind of errors it throws? Sync with Rita

                if (sseStreamReader == null)
                {
                    return;
                }

                while (!sessionTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var serverEvent = await sseStreamReader.ReadAsync();

                        if (serverEvent == null)
                        {
                            continue;
                        }

                        switch (serverEvent)
                        {
                            case IIssueChangedServerEvent issueChangedServerEvent:
                                issueChangedServerEventSourcePublisher.Publish(issueChangedServerEvent);
                                break;
                            case ITaintServerEvent taintServerEvent:
                                taintServerEventSourcePublisher.Publish(taintServerEvent);
                                break;
                        }
                    }
                    catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
                    {
                        return;
                    }
                }
            }

            public void Dispose()
            {
                sessionTokenSource.Cancel();
            }
        }
    }
}
