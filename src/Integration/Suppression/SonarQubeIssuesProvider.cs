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
using System.IO;
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

        private Dictionary<string, List<SonarQubeIssue>> suppressedModuleIssues;
        private List<IGrouping<string, SonarQubeIssue>> suppressedFileIssues;
        private bool hasModules;

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
            this.initialFetch = Task.Factory.StartNew(DoInitialFetchAsync, initialFetchCancellationTokenSource.Token);
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
            suppressedFileIssues = null;
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

            // we want a match for a file level issue (line level location or not)
            Debug.Assert(Path.IsPathRooted(filePath) && !filePath.Contains("/"), 
                $"Expecting an absolute path with only back-slashes delimiters but got '{filePath}'.");

            return this.suppressedFileIssues.FirstOrDefault(x => filePath.EndsWith(x.Key, StringComparison.OrdinalIgnoreCase))
                ?? Enumerable.Empty<SonarQubeIssue>();
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
            await SynchronizeSuppressedIssuesAsync();
        }

        private Task DoInitialFetchAsync()
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

            return SynchronizeSuppressedIssuesAsync();
        }

        private async Task SynchronizeSuppressedIssuesAsync()
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
                var moduleKeyToRelativePathToRoot = (await this.sonarQubeService.GetAllModulesAsync(sonarQubeProjectKey,
                        cancellationTokenSource.Token))
                    .ToDictionary(x => x.Key, x => NormalizeSonarQubePath(x.RelativePathToRoot));
                this.hasModules = moduleKeyToRelativePathToRoot.Keys.Count > 1;

                var allSuppressedIssues = await this.sonarQubeService.GetSuppressedIssuesAsync(sonarQubeProjectKey,
                    cancellationTokenSource.Token);

                this.suppressedModuleIssues = allSuppressedIssues.Where(x => string.IsNullOrEmpty(x.FilePath))
                    .GroupBy(x => x.ModuleKey)
                    .ToDictionary(x => x.Key, x => x.ToList());

                this.suppressedFileIssues = allSuppressedIssues.Where(x => !string.IsNullOrEmpty(x.FilePath))
                    .Select(x => new { Key = ProcessKey(moduleKeyToRelativePathToRoot, x), Issue = x })
                    .GroupBy(x => x.Key.ToUpperInvariant(), x => x.Issue)
                    .OrderByDescending(x => x.Key.Length) // We want to have the longest match first
                    .ToList();

                this.logger.WriteLine(Resources.Strings.Suppression_FinishedChecking, this.suppressedFileIssues.Count);
            }
            catch (Exception ex)
            {
                // Suppress the error - on a background thread so there isn't much else we can do
                this.logger.WriteLine(Resources.Strings.Suppressions_ERROR_Fetching, ex.Message);
            }
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
            filePathRelativeToRoot += NormalizeSonarQubePath(issue.FilePath);

            return filePathRelativeToRoot;
        }

        private string NormalizeSonarQubePath(string path)
        {
            Debug.Assert(!path.Contains("\\"), 
                $"Expecting sonarqube relative path delimiters to be forward-slash but got '{path}'.");

            return path?.Trim('/').Replace('/', '\\')
                ?? string.Empty;
        }
    }
}
