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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    /// <summary>
    /// Fetches suppressed issues from the server and updates the store
    /// </summary>
    internal interface ISuppressionIssueStoreUpdater
    {
        /// <summary>
        /// Fetches all available suppressions from the server and updates the server issues store
        /// </summary>
        Task UpdateAllServerSuppressionsAsync();

        /// <summary>
        /// Fetches suppressions from the server from the specified timestamp onwards and updates the issues store
        /// </summary>
        Task UpdateServerSuppressionsAsync(DateTimeOffset fromTimestamp);

        /// <summary>
        /// Clears all issues from the store
        /// </summary>
        void Clear();
    }

    [Export(typeof(ISuppressionIssueStoreUpdater))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SuppressionIssueStoreUpdater : ISuppressionIssueStoreUpdater, IDisposable
    {
        private readonly ISonarQubeService server;
        private readonly IServerQueryInfoProvider serverQueryInfoProvider;
        private readonly IServerIssueStoreWriter storeWriter;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;
        private readonly ICancellationTokenSourceProvider singleActiveOpCtsProvider;


        [ImportingConstructor]
        public SuppressionIssueStoreUpdater(ISonarQubeService server,
            IServerQueryInfoProvider serverQueryInfoProvider,
            IServerIssueStoreWriter storeWriter,
            ILogger logger)
            : this(server, serverQueryInfoProvider, storeWriter, logger,ThreadHandling.Instance,
                  new SingleActiveOpTokenSourceProvider(logger))
        {
        }

        internal /* for testing */ SuppressionIssueStoreUpdater(ISonarQubeService server,
            IServerQueryInfoProvider serverQueryInfoProvider,
            IServerIssueStoreWriter storeWriter,
            ILogger logger,
            IThreadHandling threadHandling,
            ICancellationTokenSourceProvider singleActiveOpCtsProvider)
        {
            this.server = server;
            this.serverQueryInfoProvider = serverQueryInfoProvider;

            this.storeWriter = storeWriter;
            this.logger = logger;
            this.threadHandling = threadHandling;
            this.singleActiveOpCtsProvider = singleActiveOpCtsProvider;
        }

        #region ISuppressionIssueStoreUpdater

        public async Task UpdateAllServerSuppressionsAsync()
        {
            await threadHandling.SwitchToBackgroundThread();

            // The CTS provider will handle cancelling the previous "fetch all"
            // if one is still in progress.
            var cts = singleActiveOpCtsProvider.Create();

            try
            {
                logger.WriteLine(Resources.Suppressions_Fetch_AllIssues);

                (string projectKey, string serverBranch) queryInfo = await serverQueryInfoProvider.GetProjectKeyAndBranchAsync(cts.Token);

                cts.Token.ThrowIfCancellationRequested();

                if (queryInfo.projectKey == null || queryInfo.serverBranch == null)
                {
                    return;
                }

                cts.Token.ThrowIfCancellationRequested();

                var allSuppressedIssues = await server.GetSuppressedIssuesAsync(queryInfo.projectKey, queryInfo.serverBranch, cts.Token);
                
                cts.Token.ThrowIfCancellationRequested();
                
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
            finally
            {
                cts.Dispose();                
            }
        }

        public Task UpdateServerSuppressionsAsync(DateTimeOffset fromTimestamp)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        #endregion

        public void Dispose() => singleActiveOpCtsProvider.Dispose();
    }

    /// <summary>
    /// Provides a testable abstraction for creating cancellation token sources.
    /// Also provides an abstraction over different CTS management strategies.
    /// </summary>
    internal interface ICancellationTokenSourceProvider : IDisposable
    {
        /// <summary>
        /// Provides a new token source for the caller to use
        /// </summary>
        /// <remarks>
        /// The caller should dispose of the token source when they have finished with it
        /// </remarks>
        CancellationTokenSource Create();
    }

    /// <summary>
    /// A CTS provider that allows only one active running operation
    /// </summary>
    /// <remarks>
    /// On the first call, <see cref="Create"/> will return a new token source.
    /// Subsequent calls will cancel the previous token before returning a new one.
    /// The class is thread-safe.
    /// </remarks>
    internal class SingleActiveOpTokenSourceProvider : ICancellationTokenSourceProvider
    {
        private CancellationTokenSource current;
        private object lockObject = new object();

        private readonly ILogger logger;

        public SingleActiveOpTokenSourceProvider(ILogger logger)
        {
            this.logger = logger;
        }

        public CancellationTokenSource Create()
        {
            lock (lockObject)
            {
                CancelCurrentOperation();              
                current = new CancellationTokenSource();
                return current;
            }
        }

        private void CancelCurrentOperation()
        {
            // We don't want multiple operations running at once.
            // If there is an operation in progress we'll cancel it.
            if (current != null && !current.IsCancellationRequested)
            {
                logger.LogVerbose(Resources.Suppressions_CancellingCurrentOperation);

                try
                {
                    current.Cancel();
                }
                catch (OperationCanceledException)
                {
                    // We have no way of telling whether the cts has already been disposed or not.
                    // If it has, calling current.Cancel() will throw. All we can do is catch and
                    // squash the exception.
                }
            }
        }

        public void Dispose() => current?.Dispose();
    }
}
