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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Reflection;
using System.Timers;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(ITelemetryManager))]
    [PartCreationPolicy(CreationPolicy.Shared)] // MEF Singleton
    public sealed class TelemetryManager : ITelemetryManager
    {
        private const int MinHoursBetweenUpload = 5;
        private const double MillisecondsToWaitBetweenUpload = 1000 * 60 * 60 * 5; // 5 hours
        private const double DelayBeforeFirstUpload = 1000 * 60 * 5; // 5 minutes

        private readonly ITelemetryDataRepository telemetryRepository;
        private readonly IActiveSolutionBoundTracker solutionBindingTracker;
        private readonly ITelemetryClient telemetryClient;
        private readonly Timer tryUploadDataTimer;
        private readonly Timer firstCallDelayer;
        private readonly string sonarLintVersion;

        public bool IsAnonymousDataShared
        {
            get { return this.telemetryRepository.Data.IsAnonymousDataShared; }
        }

        [ImportingConstructor]
        public TelemetryManager(IActiveSolutionBoundTracker solutionBindingTracker,
            ITelemetryDataRepository telemetryRepository, ITelemetryClient telemetryClient)
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

            this.solutionBindingTracker = solutionBindingTracker;
            this.telemetryClient = telemetryClient;
            this.telemetryRepository = telemetryRepository;

            this.sonarLintVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

            SaveInstallationDate();

            this.tryUploadDataTimer = new Timer()
            {
                Interval = MillisecondsToWaitBetweenUpload,
                AutoReset = true
            };

            if (IsAnonymousDataShared)
            {
                KnownUIContexts.SolutionBuildingContext.UIContextChanged += this.OnSolutionBuilding;
                this.tryUploadDataTimer.Elapsed += OnTryUploadDataTimerElapsed;
                this.tryUploadDataTimer.Start();

                this.firstCallDelayer = new Timer(DelayBeforeFirstUpload) { AutoReset = false };
                this.firstCallDelayer.Elapsed += OnTryUploadDataTimerElapsed;
                this.firstCallDelayer.Start();
            }
        }

        private async void OnTryUploadDataTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var lastUploadDate = this.telemetryRepository.Data.LastUploadDate;

            if (lastUploadDate == DateTime.MinValue ||
                IsNextDay(lastUploadDate))
            {
                this.telemetryRepository.Data.LastUploadDate = DateTime.Now;
                this.telemetryRepository.Save();

                await this.telemetryClient.SendPayload(GetPayload());
            }
        }

        private bool IsNextDay(DateTime lastUploadDate)
        {
            return (DateTime.Now - lastUploadDate).TotalDays >= 1 &&
                (DateTime.Now - lastUploadDate).TotalHours >= MinHoursBetweenUpload;
        }

        private void SaveInstallationDate()
        {
            if (this.telemetryRepository.Data.InstallationDate == DateTime.MinValue)
            {
                this.telemetryRepository.Data.InstallationDate = DateTime.Now;
                this.telemetryRepository.Save();
            }
        }

        private void OnSolutionBuilding(object sender, UIContextChangedEventArgs e)
        {
            if (!e.Activated)
            {
                return;
            }

            var lastAnalysisDate = this.telemetryRepository.Data.LastSavedAnalysisDate;
            if (lastAnalysisDate == DateTime.MinValue ||
                (DateTime.Now - lastAnalysisDate).TotalDays >= 1)
            {
                this.telemetryRepository.Data.LastSavedAnalysisDate = DateTime.Now;
                this.telemetryRepository.Data.NumberOfDaysOfUse++;
                this.telemetryRepository.Save();
            }
        }

        private TelemetryPayload GetPayload()
        {
            var numberOfDaysSinceInstallation = (long)(DateTime.Now - this.telemetryRepository.Data.InstallationDate).TotalDays;

            return new TelemetryPayload
            {
                SonarLintProduct = "SonarLint Visual Studio",
                SonarLintVersion = this.sonarLintVersion,
                NumberOfDaysSinceInstallation = numberOfDaysSinceInstallation,
                NumberOfDaysOfUse = this.telemetryRepository.Data.NumberOfDaysOfUse,
                IsUsingConnectedMode = this.solutionBindingTracker.IsActiveSolutionBound
            };
        }

        public void OptIn()
        {
            this.telemetryRepository.Data.IsAnonymousDataShared = true;
            this.telemetryRepository.Save();

            this.tryUploadDataTimer.Elapsed += OnTryUploadDataTimerElapsed;
            this.tryUploadDataTimer.Start();

            this.firstCallDelayer.Elapsed += OnTryUploadDataTimerElapsed;
            this.firstCallDelayer.Start();

            KnownUIContexts.SolutionBuildingContext.UIContextChanged += this.OnSolutionBuilding;
        }

        public async void OptOut()
        {
            this.telemetryRepository.Data.IsAnonymousDataShared = false;
            this.telemetryRepository.Save();

            this.tryUploadDataTimer.Elapsed -= OnTryUploadDataTimerElapsed;
            this.tryUploadDataTimer.Stop();

            this.firstCallDelayer.Elapsed -= OnTryUploadDataTimerElapsed;
            this.firstCallDelayer.Stop();

            KnownUIContexts.SolutionBuildingContext.UIContextChanged -= this.OnSolutionBuilding;

            await this.telemetryClient.OptOut(GetPayload());
        }

        public void Dispose()
        {
            this.tryUploadDataTimer.Elapsed -= OnTryUploadDataTimerElapsed;
            this.tryUploadDataTimer.Dispose();

            this.firstCallDelayer.Elapsed -= OnTryUploadDataTimerElapsed;
            this.firstCallDelayer.Dispose();
        }
    }
}
