/*
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
    internal interface IRoslynSuppressionUpdater
    {
        /// <summary>
        /// Fetches all available suppressions from the server and raises the <see cref="SuppressedIssuesUpdated"/> event.
        /// </summary>
        Task UpdateAllServerSuppressionsAsync();

        /// <summary>
        /// Raises the <see cref="SuppressedIssuesUpdated"/>
        /// </summary>
        Task UpdateSuppressedIssuesAsync(bool isResolved, string[] issueKeys, CancellationToken cancellationToken);

        event EventHandler<SuppressionsArgs> SuppressedIssuesUpdated;
    }

    public class SuppressionsArgs : EventArgs
    {
        public IReadOnlyList<SonarQubeIssue> SuppressedIssues { get; set; }
        public bool AreAllServerIssuesForProject { get; set; }
    }

    [Export(typeof(IRoslynSuppressionUpdater))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class RoslynIRoslynSuppressionUpdater : IRoslynSuppressionUpdater, IDisposable
    {
        private readonly ISonarQubeService server;
        private readonly IServerQueryInfoProvider serverQueryInfoProvider;
        private readonly IServerIssuesStoreWriter storeWriter;
        private readonly ICancellableActionRunner actionRunner;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public RoslynIRoslynSuppressionUpdater(
            ISonarQubeService server,
            IServerQueryInfoProvider serverQueryInfoProvider,
            IServerIssuesStoreWriter storeWriter,
            ICancellableActionRunner actionRunner,
            ILogger logger)
            : this(server, serverQueryInfoProvider, storeWriter, actionRunner, logger, ThreadHandling.Instance)
        {
        }

        internal /* for testing */ RoslynIRoslynSuppressionUpdater(
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

        #region ISuppressionIssueStoreUpdater

        public async Task UpdateAllServerSuppressionsAsync() =>
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
                            return;
                        }

                        token.ThrowIfCancellationRequested();

                        var allSuppressedIssues =
                            await server.GetSuppressedIssuesAsync(queryInfo.projectKey, queryInfo.serverBranch, null,
                                token);
                        InvokeSuppressedIssuesUpdated(allSuppressedIssues, areAllServerIssues: true);

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

        private void InvokeSuppressedIssuesUpdated(IList<SonarQubeIssue> allSuppressedIssues, bool areAllServerIssues) =>
            SuppressedIssuesUpdated?.Invoke(this, new SuppressionsArgs { SuppressedIssues = allSuppressedIssues.ToList(), AreAllServerIssuesForProject = areAllServerIssues });

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

        public event EventHandler<SuppressionsArgs> SuppressedIssuesUpdated;

        #endregion

        public void Dispose() => actionRunner.Dispose();
    }
}
