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
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Messages;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.Rules
{
    public sealed class QualityProfileProviderCachingDecorator : IQualityProfileProvider
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
            if (this.isDisposed)
            {
                return;
            }

            refreshTimer.Dispose();
            initialFetchCancellationTokenSource.Cancel();
            this.cachedQualityProfiles.Clear();
            this.isDisposed = true;
        }
    }

    public sealed class SonarQubeQualityProfileProvider : IQualityProfileProvider
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly ILogger logger;

        public SonarQubeQualityProfileProvider(ISonarQubeService sonarQubeService, ILogger logger)
        {
            if (sonarQubeService == null)
            {
                throw new ArgumentNullException(nameof(sonarQubeService));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.sonarQubeService = sonarQubeService;
            this.logger = logger;
        }

        public QualityProfile GetQualityProfile(BoundSonarQubeProject project, Language language)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            // For now only C# and VB.Net have support for the connected mode in SLVS
            if (language != Language.CSharp &&
                language != Language.VBNET)
            {
                return null;
            }

            return GetSonarQubeQualityProfile(project, language)
                .Result
                .ToRuleSet()
                .ToQualityProfile(language);
        }

        private async Task<RoslynExportProfileResponse> GetSonarQubeQualityProfile(BoundSonarQubeProject project, Language language)
        {
            var serverLanguage = language.ToServerLanguage();

            var qualityProfileInfo = await WebServiceHelper.SafeServiceCall(
                () => this.sonarQubeService.GetQualityProfileAsync(project.ProjectKey, project.Organization?.Key, serverLanguage,
                    CancellationToken.None),
                this.logger);
            if (qualityProfileInfo == null)
            {
                this.logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                   string.Format(Strings.CannotDownloadQualityProfileForLanguage, language.Name)));
                return null;
            }

            var roslynProfileExporter = await WebServiceHelper.SafeServiceCall(
                () => this.sonarQubeService.GetRoslynExportProfileAsync(qualityProfileInfo.Name, project.Organization?.Key,
                    serverLanguage, CancellationToken.None),
                this.logger);
            if (roslynProfileExporter == null)
            {
                this.logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.QualityProfileDownloadFailedMessageFormat, qualityProfileInfo.Name,
                        qualityProfileInfo.Key, language.Name)));
                return null;
            }

            return roslynProfileExporter;
        }

        public void Dispose()
        {
            // Do nothing
        }
    }
}
