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

internal interface IRoslynSuppressionUpdater
{
    /// <summary>
    /// Fetches all available suppressions from the server and raises the <see cref="SuppressedIssuesReloaded"/> event.
    /// </summary>
    Task UpdateAllServerSuppressionsAsync();

    /// <summary>
    /// Raises the <see cref="SuppressedIssuesUpdated"/> event.
    /// </summary>
    void UpdateSuppressedIssues(bool isResolved, string[] issueKeys);

    event EventHandler<SuppressionsEventArgs> SuppressedIssuesReloaded;
    event EventHandler<SuppressionsUpdateEventArgs> SuppressedIssuesUpdated;
}

public class SuppressionsEventArgs(IReadOnlyList<SonarQubeIssue> suppressedIssues) : EventArgs
{
    public IReadOnlyList<SonarQubeIssue> SuppressedIssues { get; } = suppressedIssues;
}

public class SuppressionsUpdateEventArgs(IReadOnlyList<string> suppressedIssueKeys, bool isResolved) : EventArgs
{
    public IReadOnlyList<string> SuppressedIssueKeys { get; } = suppressedIssueKeys;
    public bool IsResolved { get; } = isResolved;
}

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

    public async Task UpdateAllServerSuppressionsAsync() => await GetSuppressedIssuesAsync();

    public void UpdateSuppressedIssues(bool isResolved, string[] issueKeys)
    {
        if (!issueKeys.Any())
        {
            return;
        }
        InvokeSuppressedIssuesUpdated(issueKeys, isResolved);
    }

    public event EventHandler<SuppressionsEventArgs> SuppressedIssuesReloaded;
    public event EventHandler<SuppressionsUpdateEventArgs> SuppressedIssuesUpdated;

    public void Dispose() => actionRunner.Dispose();

    private async Task<bool> GetSuppressedIssuesAsync(string[] issueKeys = null) =>
        await threadHandling.RunOnBackgroundThread(async () =>
        {
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

                    var suppressedIssues = await server.GetSuppressedIssuesAsync(projectKey, serverBranch, issueKeys, token);
                    InvokeSuppressedIssuesReloaded(suppressedIssues);

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

            return true;
        });

    private void InvokeSuppressedIssuesReloaded(IList<SonarQubeIssue> allSuppressedIssues) => SuppressedIssuesReloaded?.Invoke(this, new SuppressionsEventArgs(allSuppressedIssues.ToList()));

    private void InvokeSuppressedIssuesUpdated(IList<string> suppressedIssueKeys, bool isResolved) =>
        SuppressedIssuesUpdated?.Invoke(this, new SuppressionsUpdateEventArgs(suppressedIssueKeys.ToList(), isResolved));
}
