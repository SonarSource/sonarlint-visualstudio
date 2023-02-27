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
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ServerSentEvents;
using SonarLint.VisualStudio.Integration;
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
        private readonly ITaintServerEventSourcePublisher taintServerEventSourcePublisher;
        private readonly IIssueServerEventSourcePublisher issueServerEventSourcePublisher;
        private readonly IThreadHandling threadHandling;

        private bool disposed;
        private readonly ILogger logger;

        [ImportingConstructor]
        public SSESessionFactory(ISonarQubeService sonarQubeClient,
            ITaintServerEventSourcePublisher taintServerEventSourcePublisher,
            IIssueServerEventSourcePublisher issueServerEventSourcePublisher,
            IThreadHandling threadHandling, 
            ILogger logger)
        {
            this.sonarQubeClient = sonarQubeClient;
            this.taintServerEventSourcePublisher = taintServerEventSourcePublisher;
            this.issueServerEventSourcePublisher = issueServerEventSourcePublisher;
            this.threadHandling = threadHandling;
            this.logger = logger;
        }

        public ISSESession Create(string projectKey)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SSESessionFactory));
            }

            var session = new SSESession(taintServerEventSourcePublisher,
                issueServerEventSourcePublisher,
                projectKey,
                threadHandling,
                sonarQubeClient,
                logger);
            
            return session;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            taintServerEventSourcePublisher.Dispose();
            issueServerEventSourcePublisher.Dispose();
            disposed = true;
        }

        internal sealed class SSESession : ISSESession
        {
            private readonly ITaintServerEventSourcePublisher taintServerEventSourcePublisher;
            private readonly IIssueServerEventSourcePublisher issueServerEventSourcePublisher;
            private readonly string projectKey;
            private readonly IThreadHandling threadHandling;
            private readonly ISonarQubeService sonarQubeService;
            private readonly ILogger logger;
            private readonly CancellationTokenSource sessionTokenSource;

            private bool disposed;

            internal SSESession(ITaintServerEventSourcePublisher taintServerEventSourcePublisher,
                IIssueServerEventSourcePublisher issueServerEventSourcePublisher,
                string projectKey,
                IThreadHandling threadHandling,
                ISonarQubeService sonarQubeService,
                ILogger logger)
            {
                this.taintServerEventSourcePublisher = taintServerEventSourcePublisher;
                this.issueServerEventSourcePublisher = issueServerEventSourcePublisher;
                this.projectKey = projectKey;
                this.threadHandling = threadHandling;
                this.sonarQubeService = sonarQubeService;
                this.logger = logger;
                this.sessionTokenSource = new CancellationTokenSource();
            }

            public async Task PumpAllAsync()
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(SSESession));
                }

                await threadHandling.SwitchToBackgroundThread();

                var sseStreamReader = await sonarQubeService.CreateSSEStreamReader(projectKey, sessionTokenSource.Token);

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
                            case ITaintServerEvent taintServerEvent:
                                taintServerEventSourcePublisher.Publish(taintServerEvent);
                                break;
                            case IIssueChangedServerEvent issueChangedServerEvent:
                                issueServerEventSourcePublisher.Publish(issueChangedServerEvent);
                                break;
                        }
                    }
                    catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                    {
                        logger.LogVerbose($"[SSESession] Failed to handle events: {ex}");
                        Dispose();
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

                disposed = true;
                sessionTokenSource.Cancel();
            }
        }
    }
}
