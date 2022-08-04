/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Suppression
{
    public sealed class SonarQubeIssuesProvider : ISonarQubeIssuesProvider
    {
        private const double MillisecondsToWaitBetweenRefresh = 1000 * 60 * 10; // 10 minutes

        private readonly TimeSpan MillisecondsToWaitForInitialFetch = TimeSpan.FromMinutes(1);
        private readonly Task initialFetch;

        private readonly ISonarQubeService sonarQubeService;
        private readonly string sonarQubeProjectKey;
        private readonly ITimer refreshTimer;
        private readonly ILogger logger;

        private Dictionary<string, List<SonarQubeIssue>> suppressedModuleIssues;
        private List<IGrouping<string, SonarQubeIssue>> suppressedFileIssues;
        private IList<SonarQubeIssue> allSuppressedIssues;
        private bool hasModules;

        private bool isDisposed;
        private CancellationTokenSource cancellationTokenSource;
        private readonly IThreadHandling threadHandling;

        public SonarQubeIssuesProvider(ISonarQubeService sonarQubeService, string sonarQubeProjectKey, ITimerFactory timerFactory,
            ILogger logger) : this(sonarQubeService, sonarQubeProjectKey, timerFactory, logger, new ThreadHandling())
        {

        }


        internal SonarQubeIssuesProvider(ISonarQubeService sonarQubeService, string sonarQubeProjectKey, ITimerFactory timerFactory,
            ILogger logger, IThreadHandling threadHandling)
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
            this.threadHandling = threadHandling;

            refreshTimer = timerFactory.Create();
            refreshTimer.AutoReset = true;
            refreshTimer.Interval = MillisecondsToWaitBetweenRefresh;
            refreshTimer.Elapsed += OnRefreshTimerElapsed;

            this.initialFetch = DoInitialFetchAsync();

            Log("Starting refresh timer");
            refreshTimer.Start();
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            refreshTimer.Dispose();
            suppressedFileIssues = null;
            allSuppressedIssues = null;
            this.isDisposed = true;
            cancellationTokenSource?.Cancel();
        }

        public IEnumerable<SonarQubeIssue> GetSuppressedIssues(string projectGuid, string filePath)
        {
            // Block the call while the cache is being built.
            // If the task has already completed then this will return immediately
            // (e.g. on subsequent calls)
            // If we time out waiting for the initial fetch then we won't suppress any issues.
            // We'll try to fetch the issues again when the timer elapses.

            var timer = Stopwatch.StartNew();
            Log("Waiting for initial fetch...");
            this.initialFetch?.Wait(MillisecondsToWaitForInitialFetch);
            Log($"Finished waiting for initial fetch. Elapsed: {timer.ElapsedMilliseconds}ms");

            if (this.suppressedFileIssues == null ||
                this.isDisposed)
            {
                return Enumerable.Empty<SonarQubeIssue>();
            }

            if (filePath == null) // we want a match for a module level issue
            {
                string moduleKey = BuildModuleKey(projectGuid);

                List<SonarQubeIssue> suppressedIssues;
                this.suppressedModuleIssues.TryGetValue(moduleKey, out suppressedIssues);

                return suppressedIssues ?? Enumerable.Empty<SonarQubeIssue>();
            }

            // We want a match for a file level issue (line level location or not)
            Debug.Assert(Path.IsPathRooted(filePath) && !filePath.Contains("/"), 
                $"Expecting an absolute path with only back-slashes delimiters but got '{filePath}'.");

            return this.suppressedFileIssues.FirstOrDefault(x => filePath.EndsWith(x.Key, StringComparison.OrdinalIgnoreCase))
                ?? Enumerable.Empty<SonarQubeIssue>();
        }

        public async Task<IEnumerable<SonarQubeIssue>> GetAllSuppressedIssuesAsync()
        {
            // Block the call while the cache is being built.
            // If the task has already completed then this will return immediately
            // (e.g. on subsequent calls)
            // If we time out waiting for the initial fetch then we won't suppress any issues.
            // We'll try to fetch the issues again when the timer elapses.

            var timer = Stopwatch.StartNew();
            Log("Waiting for initial fetch...");
            this.initialFetch?.Wait(MillisecondsToWaitForInitialFetch);
            Log($"Finished waiting for initial fetch. Elapsed: {timer.ElapsedMilliseconds}ms");

            return allSuppressedIssues ?? Enumerable.Empty<SonarQubeIssue>();
        }

        private string BuildModuleKey(string projectGuid)
        {
            if (hasModules)
            {
                // We know that the analyzer is never reporting issues on the root module as the root
                // is not associated to any msbuild project so that's why we always build a sub-module
                // key.
                return $"{sonarQubeProjectKey}:{sonarQubeProjectKey}:{projectGuid}";
            }

            // We expect the server to have moved all sub-modules issues to the root.
            return sonarQubeProjectKey;
        }

        private async void OnRefreshTimerElapsed(object sender, TimerEventArgs e)
        {
            Log("Refresh timer elapsed - synchronizing.");
            await SynchronizeSuppressedIssuesAsync();
        }

        private async Task DoInitialFetchAsync()
        {
            Log("[Initial fetch] Starting ...");
            await threadHandling.SwitchToBackgroundThread();
            Log("[Initial fetch] Switched to background thread");

            // We might not have connected to the server at this point so if necessary
            // wait before trying to fetch the issues
            Log("[Initial fetch] Checking for connection to server...");
            int retryCount = 0;
            while (!this.sonarQubeService.IsConnected && retryCount < 30)
            {
                Log($"[Initial fetch] Spinning - waiting for connection to service: {retryCount}");                
                if (this.isDisposed)
                {
                    return;
                }
                await Task.Delay(1000);
                retryCount++;
            }

            Log("[Initial fetch] Connected.");
            await SynchronizeSuppressedIssuesAsync();
            Log("[Initial fetch] Finished.");
        }

        private async Task SynchronizeSuppressedIssuesAsync()
        {
            var timer = Stopwatch.StartNew();
            Log("Synchronzing...");
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
                var moduleKeyToRelativePathToRoot = (await this.sonarQubeService.GetAllModulesAsync(sonarQubeProjectKey,
                        cancellationTokenSource.Token))
                    .ToDictionary(x => x.Key, x => x.RelativePathToRoot);
                this.hasModules = moduleKeyToRelativePathToRoot.Keys.Count > 1;
                this.allSuppressedIssues = await this.sonarQubeService.GetSuppressedIssuesAsync(sonarQubeProjectKey,
                    cancellationTokenSource.Token);
                this.suppressedModuleIssues = allSuppressedIssues.Where(x => string.IsNullOrEmpty(x.FilePath))
                    .GroupBy(x => x.ModuleKey)
                    .ToDictionary(x => x.Key, x => x.ToList());

                this.suppressedFileIssues = allSuppressedIssues.Where(x => !string.IsNullOrEmpty(x.FilePath))
                    .Select(x => new { Key = ProcessKey(moduleKeyToRelativePathToRoot, x), Issue = x })
                    .GroupBy(x => x.Key.ToUpperInvariant(), x => x.Issue)
                    .OrderByDescending(x => x.Key.Length) // We want to have the longest match first
                    .ToList();

                this.logger.WriteLine(Resources.Strings.Suppression_FinishedChecking, allSuppressedIssues.Count);
            }
            catch (Exception ex)
            {
                // Suppress the error - on a background thread so there isn't much else we can do
                this.logger.WriteLine(Resources.Strings.Suppressions_ERROR_Fetching, ex.Message);
            }
            Log($"Finished Synchronizing. Elapsed: {timer.ElapsedMilliseconds}ms");
        }

        private string ProcessKey(Dictionary<string, string> keyToPath, SonarQubeIssue issue)
        {
            // File-level issues have a file path which is relative to their modules.
            // Note that relative paths coming from SonarQube/SonarCloud always use '/' as path delimiter
            // so we need to normalize them to '\' in order to match the implementation of LiveIssue.cs

            // 1 - Find the relative path of the module to the root
            string moduleToRootRelativePath;
            keyToPath.TryGetValue(issue.ModuleKey, out moduleToRootRelativePath);

            // 2 - Append the file relative path and normalize delimiters
            var filePathRelativeToRoot = moduleToRootRelativePath != null
                ? moduleToRootRelativePath + "\\"
                : string.Empty;
            filePathRelativeToRoot += issue.FilePath;

            return filePathRelativeToRoot;
        }

        private void Log(string message, [CallerMemberName] string callerMemberName = null)
        {
            var text = $"[Suppressions] [{callerMemberName}] [Thread: {Thread.CurrentThread.ManagedThreadId}, {DateTime.Now.ToString("hh:mm:ss.fff")}]  {message}";
            logger.WriteLine(text);
        }
    }
}
