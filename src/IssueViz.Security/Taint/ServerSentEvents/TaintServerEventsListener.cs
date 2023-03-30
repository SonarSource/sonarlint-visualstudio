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

using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using System.Threading;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Integration;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;
using System;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint.ServerSentEvents
{
    /// <summary>
    /// Consumes <see cref="ITaintServerEventSource"/> and handles <see cref="ITaintServerEvent"/> coming from the server.
    /// </summary>
    internal interface ITaintServerEventsListener : IDisposable
    {
        Task ListenAsync();
    }

    [Export(typeof(ITaintServerEventsListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TaintServerEventsListener : ITaintServerEventsListener
    {
        private readonly IStatefulServerBranchProvider serverBranchProvider;
        private readonly ITaintServerEventSource taintServerEventSource;
        private readonly ITaintStore taintStore;
        private readonly IThreadHandling threadHandling;
        private readonly ITaintIssueToIssueVisualizationConverter taintToIssueVizConverter;
        private readonly ILogger logger;
        private readonly CancellationTokenSource cancellationTokenSource;

        [ImportingConstructor]
        public TaintServerEventsListener(
            IStatefulServerBranchProvider serverBranchProvider,
            ITaintServerEventSource taintServerEventSource,
            ITaintStore taintStore,
            IThreadHandling threadHandling,
            ITaintIssueToIssueVisualizationConverter taintToIssueVizConverter,
            ILogger logger)
        {
            this.serverBranchProvider = serverBranchProvider;
            this.taintServerEventSource = taintServerEventSource;
            this.taintStore = taintStore;
            this.threadHandling = threadHandling;
            this.taintToIssueVizConverter = taintToIssueVizConverter;
            this.logger = logger;

            cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task ListenAsync()
        {
            await threadHandling.SwitchToBackgroundThread();

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var taintServerEvent = await taintServerEventSource.GetNextEventOrNullAsync();

                    switch (taintServerEvent)
                    {
                        case null:
                        {
                            // Will return null when taintServerEventSource is disposed
                            return;
                        }
                        case ITaintVulnerabilityClosedServerEvent taintClosedEvent:
                        {
                            taintStore.Remove(taintClosedEvent.Key);
                            break;
                        }
                        case ITaintVulnerabilityRaisedServerEvent taintRaisedEvent:
                        {
                            await AddToStoreIfOnTheRightBranchAsync(taintRaisedEvent);
                            break;
                        }
                        default:
                        {
                            logger.LogVerbose($"[TaintServerEventsListener] Unrecognized taint event type: {taintServerEvent.GetType()}");
                            break;
                        }
                    }
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger.LogVerbose($"[TaintServerEventsListener] Failed to handle taint event: {ex}");
                }
            }
        }

        private async Task AddToStoreIfOnTheRightBranchAsync(ITaintVulnerabilityRaisedServerEvent taintRaisedEvent)
        {
            var serverBranch = await serverBranchProvider.GetServerBranchNameAsync(cancellationTokenSource.Token);

            if (taintRaisedEvent.Branch.Equals(serverBranch))
            {
                var taintIssue = taintToIssueVizConverter.Convert(taintRaisedEvent.Issue);
                taintStore.Add(taintIssue);
            }
        }

        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            disposed = true;
        }
    }
}
