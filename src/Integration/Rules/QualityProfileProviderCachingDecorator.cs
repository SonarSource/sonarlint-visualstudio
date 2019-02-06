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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.Rules
{
    public class QualityProfileProviderCachingDecorator : IQualityProfileProvider
    {
        private const double MillisecondsToWaitBetweenRefresh = 1000 * 60 * 10; // 10 minutes
        private readonly ITimer refreshTimer;

        private readonly TimeSpan MillisecondsToWaitForInitialFetch = TimeSpan.FromMinutes(1);
        private readonly CancellationTokenSource initialFetchCancellationTokenSource;
        private readonly Task initialFetch;

        private readonly IQualityProfileProvider wrappedProvider;
        private readonly BoundSonarQubeProject boundProject;
        private readonly ISonarQubeService sonarQubeService;

        private readonly IDictionary<Language, QualityProfile> cachedQualityProfiles =
            new Dictionary<Language, QualityProfile>();

        public QualityProfileProviderCachingDecorator(IQualityProfileProvider wrappedProvider, BoundSonarQubeProject boundProject,
            ISonarQubeService sonarQubeService, ITimerFactory timerFactory)
        {
            if (wrappedProvider == null)
            {
                throw new ArgumentNullException(nameof(wrappedProvider));
            }

            if (boundProject == null)
            {
                throw new ArgumentNullException(nameof(boundProject));
            }

            if (sonarQubeService == null)
            {
                throw new ArgumentNullException(nameof(sonarQubeService));
            }

            if (timerFactory == null)
            {
                throw new ArgumentNullException(nameof(timerFactory));
            }

            this.wrappedProvider = wrappedProvider;
            this.boundProject = boundProject;
            this.sonarQubeService = sonarQubeService;

            this.refreshTimer = timerFactory.Create();
            this.refreshTimer.AutoReset = true;
            this.refreshTimer.Interval = MillisecondsToWaitBetweenRefresh;
            this.refreshTimer.Elapsed += OnRefreshTimerElapsed;

            initialFetchCancellationTokenSource = new CancellationTokenSource();
            this.initialFetch = Task.Factory.StartNew(DoInitialFetch, initialFetchCancellationTokenSource.Token);
            refreshTimer.Start();
        }

        public QualityProfile GetQualityProfile(BoundSonarQubeProject project, Language language)
        {
            // Block the call while the cache is being built.
            // If the task has already completed then this will return immediately (e.g. on subsequent calls)
            // If we time out waiting for the initial fetch then we return null.
            this.initialFetch?.Wait(MillisecondsToWaitForInitialFetch);

            QualityProfile profile;
            return cachedQualityProfiles.TryGetValue(language, out profile)
                ? profile
                : null;
        }

        private void DoInitialFetch()
        {
            // We might not have connected to the server at this point so if necessary
            // wait before trying to fetch the quality profiles
            int retryCount = 0;
            while (!this.sonarQubeService.IsConnected && retryCount < 30)
            {
                if (this.initialFetchCancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                Thread.Sleep(1000);
                retryCount++;
            }

            SynchronizeQualityProfiles();
            OnInitialFetchComplete();
        }

        protected virtual void OnInitialFetchComplete()
        {
            // No-op
            // This method only exists so that tests can reliable wait until
            // the instance has been initialised.
        }

        private void OnRefreshTimerElapsed(object sender, TimerEventArgs e)
        {
            SynchronizeQualityProfiles();
        }

        private void SynchronizeQualityProfiles()
        {
            try
            {
                if (!this.sonarQubeService.IsConnected)
                {
                    // TODO: log to output window
                    Debug.Fail("Cannot synchronize suppressions - not connected to a SonarQube server");
                    return;
                }

                foreach (var language in Language.SupportedLanguages)
                {
                    this.cachedQualityProfiles[language] = this.wrappedProvider.GetQualityProfile(this.boundProject, language);
                }
            }
            catch (Exception)
            {
                // Suppress the error - on a background thread so there isn't much else we can do
            }
        }

        private bool isDisposed;
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                refreshTimer.Dispose();
                initialFetchCancellationTokenSource.Cancel();
                this.cachedQualityProfiles.Clear();
            }

            isDisposed = true;
        }
    }
}
