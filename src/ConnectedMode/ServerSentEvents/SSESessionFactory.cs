/*
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

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.Issue;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.QualityProfile;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.Taint;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ServerSentEvents;
using SonarQube.Client;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarLint.VisualStudio.ConnectedMode.ServerSentEvents
{
    internal delegate Task OnSessionFailedAsync(ISSESession failedSession);

    /// <summary>
    /// Factory for <see cref="ISSESession"/>. Responsible for disposing EventSourcePublishers <see cref="IServerSentEventSourcePublisher{T}"/>
    /// </summary>
    internal interface ISSESessionFactory : IDisposable
    {
        ISSESession Create(string projectKey, OnSessionFailedAsync onSessionFailedCallback);
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
        private readonly IQualityProfileServerEventSourcePublisher qualityProfileServerEventSourcePublisher;
        private readonly IThreadHandling threadHandling;

        private bool disposed;
        private readonly ILogger logger;

        [ImportingConstructor]
        public SSESessionFactory(ISonarQubeService sonarQubeClient,
            ITaintServerEventSourcePublisher taintServerEventSourcePublisher,
            IIssueServerEventSourcePublisher issueServerEventSourcePublisher,
            IQualityProfileServerEventSourcePublisher qualityProfileServerEventSourcePublisher, 
            IThreadHandling threadHandling, 
            ILogger logger)
        {
            this.sonarQubeClient = sonarQubeClient;
            this.taintServerEventSourcePublisher = taintServerEventSourcePublisher;
            this.issueServerEventSourcePublisher = issueServerEventSourcePublisher;
            this.qualityProfileServerEventSourcePublisher = qualityProfileServerEventSourcePublisher;
            this.threadHandling = threadHandling;
            this.logger = logger;
        }

        public ISSESession Create(string projectKey, OnSessionFailedAsync onSessionFailedCallback)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SSESessionFactory));
            }

            var session = new SSESession(taintServerEventSourcePublisher,
                issueServerEventSourcePublisher,
                qualityProfileServerEventSourcePublisher,
                projectKey,
                threadHandling,
                sonarQubeClient,
                onSessionFailedCallback,
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
            qualityProfileServerEventSourcePublisher.Dispose();
            disposed = true;
        }

        internal sealed class SSESession : ISSESession
        {
            private readonly ITaintServerEventSourcePublisher taintServerEventSourcePublisher;
            private readonly IIssueServerEventSourcePublisher issueServerEventSourcePublisher;
            private readonly IQualityProfileServerEventSourcePublisher qualityProfileServerEventSourcePublisher;
            private readonly string projectKey;
            private readonly IThreadHandling threadHandling;
            private readonly ISonarQubeService sonarQubeService;
            private readonly OnSessionFailedAsync onSessionFailedCallback;
            private readonly ILogger logger;
            private readonly CancellationTokenSource sessionTokenSource;

            private bool disposed;

            internal SSESession(ITaintServerEventSourcePublisher taintServerEventSourcePublisher,
                IIssueServerEventSourcePublisher issueServerEventSourcePublisher,
                IQualityProfileServerEventSourcePublisher qualityProfileServerEventSourcePublisher,
                string projectKey,
                IThreadHandling threadHandling,
                ISonarQubeService sonarQubeService,
                OnSessionFailedAsync onSessionFailedCallback,
                ILogger logger)
            {
                this.taintServerEventSourcePublisher = taintServerEventSourcePublisher;
                this.issueServerEventSourcePublisher = issueServerEventSourcePublisher;
                this.qualityProfileServerEventSourcePublisher = qualityProfileServerEventSourcePublisher;
                this.projectKey = projectKey;
                this.threadHandling = threadHandling;
                this.sonarQubeService = sonarQubeService;
                this.onSessionFailedCallback = onSessionFailedCallback;
                this.logger = logger;
                this.sessionTokenSource = new CancellationTokenSource();
            }

            public async Task PumpAllAsync()
            {
                if (disposed)
                {
                    logger.LogVerbose("[SSESession] Session {0} is disposed", GetHashCode());
                    throw new ObjectDisposedException(nameof(SSESession));
                }

                await threadHandling.SwitchToBackgroundThread();

                var sseStreamReader = await sonarQubeService.CreateSSEStreamReader(projectKey, sessionTokenSource.Token);

                if (sseStreamReader == null)
                {
                    logger.LogVerbose("[SSESession] Failed to create CreateSSEStreamReader");
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

                        logger.LogVerbose("[SSESession] Received server event: {0}", serverEvent.GetType());

                        switch (serverEvent)
                        {
                            case ITaintServerEvent taintServerEvent:
                            {
                                logger.LogVerbose("[SSESession] Publishing taint event...");
                                taintServerEventSourcePublisher.Publish(taintServerEvent);
                                break;
                            }
                            case IIssueChangedServerEvent issueChangedServerEvent:
                            {
                                logger.LogVerbose("[SSESession] Publishing issue changed event...");
                                issueServerEventSourcePublisher.Publish(issueChangedServerEvent);
                                break;
                            }
                            case IQualityProfileEvent qualityProfileEvent:
                            {
                                logger.LogVerbose("[SSESession] Publishing quality profile event...");
                                qualityProfileServerEventSourcePublisher.Publish(qualityProfileEvent);
                                break;
                            }
                        }
                    }
                    catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                    {
                        logger.LogVerbose($"[SSESession] Failed to handle events: {ex}");
                        onSessionFailedCallback(this).Forget();
                        Dispose();
                        return;
                    }
                }

                logger.LogVerbose("[SSESession] Session stopped, session token was canceled");
            }

            public void Dispose()
            {
                logger.LogVerbose("[SSESession] Disposing session: {0}", GetHashCode());

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
