﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.ConnectedMode.Helpers;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    /// <summary>
    /// Fetches suppressed issues from the server and updates the store
    /// </summary>
    internal interface IRoslynSuppressionUpdater
    {
        /// <summary>
        /// Fetches all available suppressions from the server and updates the server issues store
        /// </summary>
        Task UpdateAllServerSuppressionsAsync();

        /// <summary>
        /// Updates the suppression status of the given issue key(s). If the issues are not found locally, they are fetched.
        /// </summary>
        Task UpdateSuppressedIssuesAsync(bool isResolved, string[] issueKeys, CancellationToken cancellationToken);

        event EventHandler<SuppressionsEventArgs> SuppressedIssuesReloaded;
        event EventHandler<SuppressionsEventArgs> NewIssuesSuppressed;
        event EventHandler<SuppressionsUpdateEventArgs> NewIssuesResolved;
    }

    public class SuppressionsEventArgs(IReadOnlyList<SonarQubeIssue> suppressedIssues) : EventArgs
    {
        public IReadOnlyList<SonarQubeIssue> SuppressedIssues { get; } = suppressedIssues;
    }

    public class SuppressionsUpdateEventArgs(IReadOnlyList<string> suppressedIssueKeys) : EventArgs
    {
        public IReadOnlyList<string> SuppressedIssueKeys { get; } = suppressedIssueKeys;
    }

    [Export(typeof(IRoslynSuppressionUpdater))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class RoslynSuppressionUpdater : IRoslynSuppressionUpdater, IDisposable
    {
        private readonly ISonarQubeService server;
        private readonly IServerQueryInfoProvider serverQueryInfoProvider;
        private readonly IServerIssuesStoreWriter storeWriter;
        private readonly ICancellableActionRunner actionRunner;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public RoslynSuppressionUpdater(
            ISonarQubeService server,
            IServerQueryInfoProvider serverQueryInfoProvider,
            IServerIssuesStoreWriter storeWriter,
            ICancellableActionRunner actionRunner,
            ILogger logger)
            : this(server, serverQueryInfoProvider, storeWriter, actionRunner, logger, ThreadHandling.Instance)
        {
        }

        internal /* for testing */ RoslynSuppressionUpdater(
            ISonarQubeService server,
            IServerQueryInfoProvider serverQueryInfoProvider,
            IServerIssuesStoreWriter storeWriter,
            ICancellableActionRunner actionRunner,
            ILogger logger,
            IThreadHandling threadHandling)
        {
            this.server = server;
            this.serverQueryInfoProvider = serverQueryInfoProvider;
            this.storeWriter = storeWriter;
            this.actionRunner = actionRunner;
            this.logger = logger;
            this.threadHandling = threadHandling;
        }

        #region IRoslynSuppressionUpdater

        public async Task UpdateAllServerSuppressionsAsync()
        {
            await threadHandling.RunOnBackgroundThread(async () =>
            {
                await actionRunner.RunAsync(async token =>
                {
                    try
                    {
                        logger.WriteLine(Resources.Suppressions_Fetch_AllIssues);

                        (string projectKey, string serverBranch) queryInfo =
                            await serverQueryInfoProvider.GetProjectKeyAndBranchAsync(token);

                        if (queryInfo.projectKey == null || queryInfo.serverBranch == null)
                        {
                            storeWriter.Reset();
                            return;
                        }

                        token.ThrowIfCancellationRequested();

                        var allSuppressedIssues =
                            await server.GetSuppressedIssuesAsync(queryInfo.projectKey, queryInfo.serverBranch, null,
                                token);
                        storeWriter.AddIssues(allSuppressedIssues, clearAllExistingIssues: true);

                        logger.WriteLine(Resources.Suppression_Fetch_AllIssues_Finished);
                    }
                    catch (OperationCanceledException)
                    {
                        logger.WriteLine(Resources.Suppressions_FetchOperationCancelled);
                    }
                    catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                    {
                        logger.LogVerbose(Resources.Suppression_FetchError_Verbose, ex);
                        logger.WriteLine(Resources.Suppressions_FetchError_Short, ex.Message);
                    }
                });

                return true;
            });
        }

        public async Task UpdateSuppressedIssuesAsync(bool isResolved, string[] issueKeys, CancellationToken cancellationToken)
        {
            if (!issueKeys.Any())
            {
                return;
            }

            await threadHandling.SwitchToBackgroundThread();

            try
            {
                var existingIssuesInStore = storeWriter.Get();
                var missingIssueKeys = issueKeys.Where(x => existingIssuesInStore.All(y => !y.IssueKey.Equals(x, StringComparison.Ordinal))).ToArray();

                // Fetch only missing suppressed issues
                if (isResolved && missingIssueKeys.Any())
                {
                    var queryInfo = await serverQueryInfoProvider.GetProjectKeyAndBranchAsync(cancellationToken);
                    var issues = await server.GetSuppressedIssuesAsync(
                        queryInfo.projectKey,
                        queryInfo.branchName,
                        missingIssueKeys,
                        cancellationToken);

                    storeWriter.AddIssues(issues, clearAllExistingIssues: false);
                }

                storeWriter.UpdateIssues(isResolved, issueKeys);
            }
            catch (OperationCanceledException)
            {
                logger.WriteLine(Resources.Suppressions_UpdateOperationCancelled);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogVerbose(Resources.Suppression_UpdateError_Verbose, ex);
                logger.WriteLine(Resources.Suppressions_UpdateError_Short, ex.Message);
            }
        }

        public event EventHandler<SuppressionsEventArgs> SuppressedIssuesReloaded;
        public event EventHandler<SuppressionsEventArgs> NewIssuesSuppressed;
        public event EventHandler<SuppressionsUpdateEventArgs> NewIssuesResolved;

        #endregion

        public void Dispose() => actionRunner.Dispose();
    }
}
