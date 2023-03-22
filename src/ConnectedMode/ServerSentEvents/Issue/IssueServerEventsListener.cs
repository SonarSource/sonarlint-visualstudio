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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.Issue
{
    /// <summary>
    /// Handles <see cref="IIssueServerEvent"/> coming from the server.
    /// </summary>
    internal interface IIssueServerEventsListener : IDisposable
    {
        Task ListenAsync();
    }

    [Export(typeof(IIssueServerEventsListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class IssueServerEventsListener : IIssueServerEventsListener
    {
        private readonly IIssueServerEventSource issueServerEventSource;
        private readonly ISuppressionIssueStoreUpdater suppressionIssueStoreUpdater;
        private readonly IThreadHandling threadHandling;
        private readonly IStatefulServerBranchProvider branchProvider;
        private readonly ILogger logger;
        private readonly CancellationTokenSource cancellationTokenSource;

        [ImportingConstructor]
        public IssueServerEventsListener(IIssueServerEventSource issueServerEventSource,
            ISuppressionIssueStoreUpdater suppressionIssueStoreUpdater,
            IStatefulServerBranchProvider branchProvider,
            IThreadHandling threadHandling,
            ILogger logger)
        {
            this.issueServerEventSource = issueServerEventSource;
            this.suppressionIssueStoreUpdater = suppressionIssueStoreUpdater;
            this.branchProvider = branchProvider;
            this.threadHandling = threadHandling;
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
                    var issueServerEvent = await issueServerEventSource.GetNextEventOrNullAsync();

                    if (issueServerEvent == null)
                    {
                        // Will return null when issueServerEventSource is disposed
                        return;
                    }

                    var serverBranch = await branchProvider.GetServerBranchNameAsync(cancellationTokenSource.Token);

                    var issueKeysForCurrentBranch = issueServerEvent.BranchAndIssueKeys
                        .Where(x => x.BranchName.Equals(serverBranch))
                        .Select(x => x.IssueKey)
                        .ToArray();

                    logger.LogVerbose(Resources.Suppression_IssueChangedEvent, issueServerEvent.IsResolved, string.Join(",", issueKeysForCurrentBranch));

                    if (issueKeysForCurrentBranch.Any())
                    {
                        await suppressionIssueStoreUpdater.UpdateSuppressedIssuesAsync(issueServerEvent.IsResolved, issueKeysForCurrentBranch, cancellationTokenSource.Token);
                    }

                    logger.LogVerbose(Resources.Suppression_IssueChangedEventFinished);
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger.LogVerbose($"[IssueServerEventsListener] Failed to handle issue event: {ex}");
                }
            }
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource?.Dispose();
        }
    }
}
