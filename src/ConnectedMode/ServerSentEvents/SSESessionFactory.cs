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
using SonarLint.VisualStudio.Core.ServerSentEvents;
using SonarLint.VisualStudio.Core.ServerSentEvents.Issues;
using SonarLint.VisualStudio.Core.ServerSentEvents.TaintVulnerabilities;
using SonarQube.Client;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarLint.VisualStudio.ConnectedMode.ServerSentEvents
{
    /// <summary>
    /// Factory for <see cref="ISSESession"/>. Responsible for disposing EventSourcePublishers <see cref="IServerSentEventSourcePublisher{T}"/>
    /// </summary>
    internal interface ISSESessionFactory : IDisposable
    {
        ISSESession Create(string projectKey);
    }

    /// <summary>
    /// Represents the session entity, that is responsible for dealing with <see cref="ISSEStreamReader"/> reader
    /// and propagating events to correct topic event publishers
    /// </summary>
    internal interface ISSESession : IDisposable
    {
        Task PumpAllAsync();
    }

    [Export(typeof(ISSESessionFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SSESessionFactory : ISSESessionFactory
    {
        private readonly ISonarQubeService sonarQubeClient;
        private readonly IIssueChangedServerEventSourcePublisher issueChangedServerEventSourcePublisher;
        private readonly ITaintServerEventSourcePublisher taintServerEventSourcePublisher;
        private readonly IThreadHandling threadHandling;

        private bool disposed;

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
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SSESessionFactory));
            }

            var session = new SSESession(issueChangedServerEventSourcePublisher,
                taintServerEventSourcePublisher,
                projectKey,
                threadHandling,
                sonarQubeClient);
            
            return session;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            issueChangedServerEventSourcePublisher.Dispose();
            taintServerEventSourcePublisher.Dispose();
            disposed = true;
        }

        internal sealed class SSESession : ISSESession
        {
            private readonly IIssueChangedServerEventSourcePublisher issueChangedServerEventSourcePublisher;
            private readonly ITaintServerEventSourcePublisher taintServerEventSourcePublisher;
            private readonly string projectKey;
            private readonly IThreadHandling threadHandling;
            private readonly ISonarQubeService sonarQubeService;
            private readonly CancellationTokenSource sessionTokenSource;

            private bool disposed;

            internal SSESession(IIssueChangedServerEventSourcePublisher issueChangedServerEventSourcePublisher, 
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
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(SSESession));
                }

                await threadHandling.SwitchToBackgroundThread();

                var sseStream = await sonarQubeService.CreateServerSentEventsStream(projectKey, sessionTokenSource.Token);

                if (sseStream == null)
                {
                    return;
                }

                sseStream.BeginListening().Forget();

                while (!sessionTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var serverEvent = await sseStream.ReadAsync();

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
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                }
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                sessionTokenSource.Cancel();
                disposed = true;
            }
        }
    }
}
