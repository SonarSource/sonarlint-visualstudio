/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Timers;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration
{
    public sealed class TelemetryManager : ITelemetryManager, IDisposable
    {
        private const double DelayBeforeFirstUpload = 1000 * 60 * 5;// 5 minutes
        private const double MillisecondsToWaitBetweenUpload = 1000 * 60 * 60 * 5; // 5 hours
        private const int MinHoursBetweenUpload = 5;

        internal static readonly string SonarLintVersion = GetSonarLintVersion();
        internal static readonly string VisualStudioVersion = GetVisualStudioVersion();

        private readonly ITimer firstCallDelayer;
        private readonly IActiveSolutionBoundTracker solutionBindingTracker;
        private readonly ITelemetryClient telemetryClient;
        private readonly ITelemetryDataRepository telemetryRepository;
        private readonly ITimer tryUploadDataTimer;
        private readonly IKnownUIContexts knownUIContexts;

        public TelemetryManager(IActiveSolutionBoundTracker solutionBindingTracker, ITelemetryDataRepository telemetryRepository,
            ITelemetryClient telemetryClient, ITimerFactory timerFactory, IKnownUIContexts knownUIContexts)
        {
            if (solutionBindingTracker == null)
            {
                throw new ArgumentNullException(nameof(solutionBindingTracker));
            }
            if (telemetryRepository == null)
            {
                throw new ArgumentNullException(nameof(telemetryRepository));
            }
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }
            if (timerFactory == null)
            {
                throw new ArgumentNullException(nameof(timerFactory));
            }
            if (knownUIContexts == null)
            {
                throw new ArgumentNullException(nameof(knownUIContexts));
            }

            this.solutionBindingTracker = solutionBindingTracker;
            this.telemetryClient = telemetryClient;
            this.telemetryRepository = telemetryRepository;
            this.knownUIContexts = knownUIContexts;

            if (this.telemetryRepository.Data.InstallationDate == DateTime.MinValue)
            {
                this.telemetryRepository.Data.InstallationDate = DateTime.Now; // TODO: Use some mockable clock
                this.telemetryRepository.Save();
            }

            this.firstCallDelayer = timerFactory.Create();
            this.firstCallDelayer.AutoReset = false;
            this.firstCallDelayer.Interval = DelayBeforeFirstUpload;
            this.tryUploadDataTimer = timerFactory.Create();
            this.tryUploadDataTimer.AutoReset = true;
            this.tryUploadDataTimer.Interval = MillisecondsToWaitBetweenUpload;

            if (IsAnonymousDataShared)
            {
                EnableAllEvents();
            }
        }

        public bool IsAnonymousDataShared => this.telemetryRepository.Data.IsAnonymousDataShared;

        public void Dispose()
        {
            DisableAllEvents();

            (this.tryUploadDataTimer as IDisposable)?.Dispose();
            (this.firstCallDelayer as IDisposable)?.Dispose();
            this.telemetryClient.Dispose();
            this.telemetryRepository.Dispose();
        }

        public void OptIn()
        {
            this.telemetryRepository.Data.IsAnonymousDataShared = true;
            this.telemetryRepository.Save();

            EnableAllEvents();
        }

        public async void OptOut()
        {
            this.telemetryRepository.Data.IsAnonymousDataShared = false;
            this.telemetryRepository.Save();

            DisableAllEvents();

            await this.telemetryClient.OptOut(GetPayload());
        }

        private static string GetSonarLintVersion()
        {
            return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        }

        private static string GetVisualStudioVersion()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "msenv.dll");
            return File.Exists(path)
                ? FileVersionInfo.GetVersionInfo(path).ProductVersion
                : string.Empty;
        }

        private static bool IsNextDay(DateTime lastUploadDate)
        {
            return (DateTime.Now - lastUploadDate).TotalDays >= 1 &&
                (DateTime.Now - lastUploadDate).TotalHours >= MinHoursBetweenUpload;
        }

        private void DisableAllEvents()
        {
            this.tryUploadDataTimer.Elapsed -= OnTryUploadDataTimerElapsed;
            this.tryUploadDataTimer.Stop();

            this.firstCallDelayer.Elapsed -= OnTryUploadDataTimerElapsed;
            this.firstCallDelayer.Stop();

            this.knownUIContexts.SolutionBuildingContextChanged -= this.OnAnalysisRun;
            this.knownUIContexts.SolutionExistsAndFullyLoadedContextChanged -= this.OnAnalysisRun;
        }

        private void EnableAllEvents()
        {
            this.tryUploadDataTimer.Elapsed += OnTryUploadDataTimerElapsed;
            this.tryUploadDataTimer.Start();

            this.firstCallDelayer.Elapsed += OnTryUploadDataTimerElapsed;
            this.firstCallDelayer.Start();

            this.knownUIContexts.SolutionBuildingContextChanged += this.OnAnalysisRun;
            this.knownUIContexts.SolutionExistsAndFullyLoadedContextChanged += this.OnAnalysisRun;
        }

        private TelemetryPayload GetPayload()
        {
            var numberOfDaysSinceInstallation = (long)(DateTime.Now - this.telemetryRepository.Data.InstallationDate).TotalDays;
            return new TelemetryPayload
            {
                SonarLintProduct = "SonarLint Visual Studio",
                SonarLintVersion = SonarLintVersion,
                VisualStudioVersion = VisualStudioVersion,
                NumberOfDaysSinceInstallation = numberOfDaysSinceInstallation,
                NumberOfDaysOfUse = this.telemetryRepository.Data.NumberOfDaysOfUse,
                IsUsingConnectedMode = this.solutionBindingTracker.IsActiveSolutionBound
            };
        }

        private void OnAnalysisRun(object sender, UIContextChangedEventArgs e)
        {
            if (!e.Activated)
            {
                return;
            }

            var lastAnalysisDate = this.telemetryRepository.Data.LastSavedAnalysisDate;
            if (lastAnalysisDate != DateTime.MinValue && (DateTime.Now - lastAnalysisDate).TotalDays < 1)
            {
                return;
            }

            this.telemetryRepository.Data.LastSavedAnalysisDate = DateTime.Now;
            this.telemetryRepository.Data.NumberOfDaysOfUse++;
            this.telemetryRepository.Save();
        }

        private async void OnTryUploadDataTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var lastUploadDate = this.telemetryRepository.Data.LastUploadDate;
            if (lastUploadDate != DateTime.MinValue && !IsNextDay(lastUploadDate))
            {
                return;
            }

            this.telemetryRepository.Data.LastUploadDate = DateTime.Now;
            this.telemetryRepository.Save();

            await this.telemetryClient.SendPayload(GetPayload());
        }
    }
}
