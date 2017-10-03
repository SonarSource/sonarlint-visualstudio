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
        internal static readonly string SonarLintVersion = GetSonarLintVersion();
        internal static readonly string VisualStudioVersion = GetVisualStudioVersion();

        private readonly IActiveSolutionBoundTracker solutionBindingTracker;
        private readonly ITelemetryClient telemetryClient;
        private readonly ITelemetryTimer telemetryTimer;
        private readonly ITelemetryDataRepository telemetryRepository;
        private readonly IKnownUIContexts knownUIContexts;

        public TelemetryManager(IActiveSolutionBoundTracker solutionBindingTracker, ITelemetryDataRepository telemetryRepository,
            ITelemetryClient telemetryClient, ITelemetryTimer telemetryTimer, IKnownUIContexts knownUIContexts)
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
            if (telemetryTimer == null)
            {
                throw new ArgumentNullException(nameof(telemetryTimer));
            }
            if (knownUIContexts == null)
            {
                throw new ArgumentNullException(nameof(knownUIContexts));
            }

            this.solutionBindingTracker = solutionBindingTracker;
            this.telemetryClient = telemetryClient;
            this.telemetryTimer = telemetryTimer;
            this.telemetryRepository = telemetryRepository;
            this.knownUIContexts = knownUIContexts;

            if (this.telemetryRepository.Data.InstallationDate == DateTime.MinValue)
            {
                this.telemetryRepository.Data.InstallationDate = DateTime.Now; // TODO: Use some mockable clock
                this.telemetryRepository.Save();
            }

            if (IsAnonymousDataShared)
            {
                EnableAllEvents();
            }
        }

        public bool IsAnonymousDataShared => this.telemetryRepository.Data.IsAnonymousDataShared;

        public void Dispose()
        {
            DisableAllEvents();

            this.telemetryTimer.Dispose();
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

        private void DisableAllEvents()
        {
            this.telemetryTimer.Elapsed -= OnTryUploadDataTimerElapsed;
            this.telemetryTimer.Stop();

            this.knownUIContexts.SolutionBuildingContextChanged -= this.OnAnalysisRun;
            this.knownUIContexts.SolutionExistsAndFullyLoadedContextChanged -= this.OnAnalysisRun;
        }

        private void EnableAllEvents()
        {
            this.telemetryTimer.Elapsed += OnTryUploadDataTimerElapsed;
            this.telemetryTimer.Start();

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

            var lastAnalysisDate = this.telemetryRepository.Data.LastSavedAnalysisDate.Date;
            if (DateTime.Today.Subtract(lastAnalysisDate).TotalDays >= 1)
            {
                this.telemetryRepository.Data.LastSavedAnalysisDate = DateTime.Today;
                this.telemetryRepository.Data.NumberOfDaysOfUse++;
                this.telemetryRepository.Save();
            }
        }

        private async void OnTryUploadDataTimerElapsed(object sender, TelemetryTimerEventArgs e)
        {
            this.telemetryRepository.Data.LastUploadDate = e.SignalTime;
            this.telemetryRepository.Save();

            await this.telemetryClient.SendPayload(GetPayload());
        }
    }
}
