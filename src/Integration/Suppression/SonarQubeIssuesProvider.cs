/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.Suppression
{
    public sealed class SonarQubeIssuesProvider : ISonarQubeIssuesProvider, IDisposable
    {
        private const double MillisecondsToWaitBetweenRefresh = 1000 * 60 * 10; // 10 minutes

        private readonly TimeSpan MillisecondsToWaitForInitialFetch = TimeSpan.FromMinutes(1);
        private readonly Task initialFetch;

        private readonly ISonarQubeService sonarQubeService;
        private readonly string sonarQubeProjectKey;
        private readonly ITimer refreshTimer;
        private readonly ILogger logger;
        private readonly CancellationTokenSource initialFetchCancellationTokenSource;

        private IList<SonarQubeIssue> cachedSuppressedIssues;
        private bool isDisposed;
        private CancellationTokenSource cancellationTokenSource;

        public SonarQubeIssuesProvider(ISonarQubeService sonarQubeService, string sonarQubeProjectKey, ITimerFactory timerFactory,
            ILogger logger)
        {
            if (sonarQubeService == null)
            {
                throw new ArgumentNullException(nameof(sonarQubeService));
            }
            if (string.IsNullOrWhiteSpace(sonarQubeProjectKey))
            {
                throw new ArgumentNullException(nameof(sonarQubeProjectKey));
            }
            if (timerFactory == null)
            {
                throw new ArgumentNullException(nameof(timerFactory));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.sonarQubeService = sonarQubeService;
            this.sonarQubeProjectKey = sonarQubeProjectKey;
            this.logger = logger;

            refreshTimer = timerFactory.Create();
            refreshTimer.AutoReset = true;
            refreshTimer.Interval = MillisecondsToWaitBetweenRefresh;
            refreshTimer.Elapsed += OnRefreshTimerElapsed;

            initialFetchCancellationTokenSource = new CancellationTokenSource();
            this.initialFetch = Task.Factory.StartNew(DoInitialFetch, initialFetchCancellationTokenSource.Token);
            refreshTimer.Start();
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            refreshTimer.Dispose();
            initialFetchCancellationTokenSource.Cancel();
            cachedSuppressedIssues = null;
            this.isDisposed = true;
        }

        public IEnumerable<SonarQubeIssue> GetSuppressedIssues(string projectGuid, string filePath)
        {
            // Block the call while the cache is being built.
            // If the task has already completed then this will return immediately
            // (e.g. on subsequent calls)
            // If we time out waiting for the initial fetch then we won't suppress any issues.
            // We'll try to fetch the issues again when the timer elapses.
            this.initialFetch?.Wait(MillisecondsToWaitForInitialFetch);

            if (this.cachedSuppressedIssues == null || this.isDisposed)
            {
                return Enumerable.Empty<SonarQubeIssue>();
            }

            string moduleKey = BuildModuleKey(projectGuid);
            return this.cachedSuppressedIssues.Where(x =>
                x.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) &&
                x.ModuleKey.Equals(moduleKey, StringComparison.OrdinalIgnoreCase));
        }

        private string BuildModuleKey(string projectGuid)
        {
            return $"{sonarQubeProjectKey}:{sonarQubeProjectKey}:{projectGuid}";
        }

        private async void OnRefreshTimerElapsed(object sender, TimerEventArgs e)
        {
            await SynchronizeSuppressedIssues();
        }

        private Task DoInitialFetch()
        {
            // We might not have connected to the server at this point so if necessary
            // wait before trying to fetch the issues
            int retryCount = 0;
            while (!this.sonarQubeService.IsConnected && retryCount < 30)
            {
                if (this.initialFetchCancellationTokenSource.IsCancellationRequested)
                {
                    return Task.CompletedTask;
                }
                Thread.Sleep(1000);
                retryCount++;
            }

            return SynchronizeSuppressedIssues();
        }

        private async Task SynchronizeSuppressedIssues()
        {
            try
            {
                if (!this.sonarQubeService.IsConnected)
                {

                    this.logger.WriteLine(Resources.Strings.Suppressions_NotConnected);
                    return;
                }
                this.logger.WriteLine(Resources.Strings.Suppression_Checking);
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = new CancellationTokenSource();

                // TODO: Handle race conditions
                this.cachedSuppressedIssues = await this.sonarQubeService.GetSuppressedIssuesAsync(
                    sonarQubeProjectKey, cancellationTokenSource.Token);

                this.logger.WriteLine(Resources.Strings.Suppression_FinishedChecking, this.cachedSuppressedIssues.Count);
            }
            catch (Exception ex)
            {
                // Suppress the error - on a background thread so there isn't much else we can do
                this.logger.WriteLine(Resources.Strings.Suppressions_ERROR_Fetching, ex.Message);
            }
        }
    }
}
