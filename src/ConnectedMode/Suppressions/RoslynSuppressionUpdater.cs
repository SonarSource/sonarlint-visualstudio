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

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions;

[Export(typeof(IRoslynSuppressionUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class RoslynSuppressionUpdater : IRoslynSuppressionUpdater, IDisposable
{
    private readonly ICancellableActionRunner actionRunner;
    private readonly ILogger logger;
    private readonly ISonarQubeService server;
    private readonly IServerQueryInfoProvider serverQueryInfoProvider;
    private readonly IThreadHandling threadHandling;

    [ImportingConstructor]
    public RoslynSuppressionUpdater(
        ISonarQubeService server,
        IServerQueryInfoProvider serverQueryInfoProvider,
        IServerIssuesStoreWriter storeWriter,
        ICancellableActionRunner actionRunner,
        ILogger logger)
        : this(server, serverQueryInfoProvider, actionRunner, logger, ThreadHandling.Instance)
    {
    }

    internal RoslynSuppressionUpdater(
        ISonarQubeService server,
        IServerQueryInfoProvider serverQueryInfoProvider,
        ICancellableActionRunner actionRunner,
        ILogger logger,
        IThreadHandling threadHandling)
    {
        this.server = server;
        this.serverQueryInfoProvider = serverQueryInfoProvider;
        this.actionRunner = actionRunner;
        this.logger = logger.ForContext(nameof(RoslynSuppressionUpdater));
        this.threadHandling = threadHandling;
    }

    public async Task UpdateAllServerSuppressionsAsync()
    {
        var suppressedIssues = await GetSuppressedIssuesAsync();
        if (suppressedIssues?.Count > 0)
        {
            InvokeSuppressedIssuesReloaded(suppressedIssues);
        }
    }

    public async Task UpdateSuppressedIssuesAsync(bool isResolved, string[] issueKeys, CancellationToken cancellationToken)
    {
        if (!issueKeys.Any())
        {
            return;
        }
        if (isResolved)
        {
            InvokeSuppressionsRemoved(issueKeys);
            return;
        }

        var suppressedIssues = await GetSuppressedIssuesAsync(issueKeys, cancellationToken);
        if (suppressedIssues?.Count > 0)
        {
            InvokeNewIssuesSuppressed(suppressedIssues);
        }
    }

    public event EventHandler<SuppressionsEventArgs> SuppressedIssuesReloaded;
    public event EventHandler<SuppressionsEventArgs> NewIssuesSuppressed;
    public event EventHandler<SuppressionsRemovedEventArgs> SuppressionsRemoved;

    public void Dispose() => actionRunner.Dispose();

    private async Task<IList<SonarQubeIssue>> GetSuppressedIssuesAsync(string[] issueKeys = null, CancellationToken? cancellationToken = null) =>
        await threadHandling.RunOnBackgroundThread(async () =>
        {
            IList<SonarQubeIssue> suppressedIssues = null;
            await actionRunner.RunAsync(async token =>
            {
                try
                {
                    var allServerIssuesFetched = issueKeys == null;
                    logger.WriteLine(Resources.Suppressions_Fetch_Issues, allServerIssuesFetched);

                    var (projectKey, serverBranch) = await serverQueryInfoProvider.GetProjectKeyAndBranchAsync(token);
                    if (projectKey == null || serverBranch == null)
                    {
                        return;
                    }
                    token.ThrowIfCancellationRequested();
                    cancellationToken?.ThrowIfCancellationRequested();

                    suppressedIssues = await server.GetSuppressedIssuesAsync(projectKey, serverBranch, issueKeys, token);

                    logger.WriteLine(Resources.Suppression_Fetch_Issues_Finished, allServerIssuesFetched);
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

            return suppressedIssues;
        });

    private void InvokeSuppressedIssuesReloaded(IList<SonarQubeIssue> allSuppressedIssues) => SuppressedIssuesReloaded?.Invoke(this, new SuppressionsEventArgs(allSuppressedIssues.ToList()));

    private void InvokeSuppressionsRemoved(IList<string> suppressedIssueKeys) => SuppressionsRemoved?.Invoke(this, new SuppressionsRemovedEventArgs(suppressedIssueKeys.ToList()));

    private void InvokeNewIssuesSuppressed(IList<SonarQubeIssue> newSuppressedIssues) => NewIssuesSuppressed?.Invoke(this, new SuppressionsEventArgs(newSuppressedIssues.ToList()));
}
