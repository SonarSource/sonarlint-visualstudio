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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;

namespace SonarLint.VisualStudio.ConnectedMode;

/// <summary>
/// Higher-level class that raises Git events for bound solutions
/// </summary>
/// <remarks>
/// This class handles changing the repo being monitored when a solution is opened/closed
/// </remarks>
[Export(typeof(IBoundSolutionGitMonitor))]
// Singleton - stateful.
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class BoundSolutionGitMonitor : IBoundSolutionGitMonitor
{
    public event EventHandler HeadChanged;

    /// <summary>
    /// Factory to create a new object that will monitor the local git repo
    /// for relevant changes
    /// </summary>
    internal /* for testing */ delegate IGitEvents GitEventFactory(string repoPathRoot);

    private static readonly GitEventFactory CreateGitEvents = repoRootPath => new GitEventsMonitor(repoRootPath);

    private readonly IGitWorkspaceService gitWorkspaceService;
    private readonly GitEventFactory createLocalGitMonitor;
    private readonly ILogger logger;
    private readonly object lockObject = new();

    private IGitEvents currentRepoEvents;
    private bool disposed;

    public IInitializationProcessor InitializationProcessor { get; }

    [ImportingConstructor]
    public BoundSolutionGitMonitor(IGitWorkspaceService gitWorkspaceService, IInitializationProcessorFactory initializationProcessorFactory, ILogger logger)
        : this(gitWorkspaceService, logger, initializationProcessorFactory, CreateGitEvents)
    {
    }

    internal /* for testing */ BoundSolutionGitMonitor(
        IGitWorkspaceService gitWorkspaceService,
        ILogger logger,
        IInitializationProcessorFactory initializationProcessorFactory,
        GitEventFactory gitEventFactory)
    {
        this.gitWorkspaceService = gitWorkspaceService;
        this.logger = logger;
        createLocalGitMonitor = gitEventFactory;
        InitializationProcessor = initializationProcessorFactory.CreateAndStart<BoundSolutionGitMonitor>([], () =>
        {
            if (disposed)
            {
                return;
            }

            RefreshInternal();
        });
    }

    public void Refresh()
    {
        if (!InitializationProcessor.IsFinalized)
        {
            return;
        }

        if (disposed)
        {
            throw new ObjectDisposedException(nameof(BoundSolutionGitMonitor));
        }

        RefreshInternal();
    }

    private void OnHeadChanged(object sender, EventArgs e)
    {
        // Forward the notification that the local repo head has changed
        try
        {
            logger.LogVerbose(Resources.GitMonitor_GitBranchChanged);
            HeadChanged?.Invoke(this, e);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Resources.GitMonitor_EventError, ex);
        }
    }

    private void RefreshInternal()
    {
        var rootPath = gitWorkspaceService.GetRepoRoot();

        if (rootPath == null)
        {
            logger.LogVerbose(Resources.GitMonitor_NoRepo);
        }
        else
        {
            logger.LogVerbose(Resources.GitMonitor_MonitoringRepoStarted, rootPath);
            UpdateCurrentRepoEvents(createLocalGitMonitor(rootPath));
        }
    }

    private void UpdateCurrentRepoEvents(IGitEvents value)
    {
        lock (lockObject)
        {
            if (currentRepoEvents != null)
            {
                logger.LogVerbose(Resources.GitMonitor_MonitoringRepoStopped);
                currentRepoEvents.HeadChanged -= OnHeadChanged;
                currentRepoEvents.Dispose();
            }

            currentRepoEvents = value;

            if (currentRepoEvents != null)
            {
                currentRepoEvents.HeadChanged += OnHeadChanged;
            }
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        UpdateCurrentRepoEvents(null);
        disposed = true;
    }
}
