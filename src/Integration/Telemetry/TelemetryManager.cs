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
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration
{
    public sealed class TelemetryManager : ITelemetryManager, IDisposable
    {
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

        public bool IsAnonymousDataShared => telemetryRepository.Data.IsAnonymousDataShared;

        public void Dispose()
        {
            DisableAllEvents();

            (telemetryTimer as IDisposable)?.Dispose();
            (telemetryClient as IDisposable)?.Dispose();
            (telemetryRepository as IDisposable)?.Dispose();
        }

        public void OptIn()
        {
            telemetryRepository.Data.IsAnonymousDataShared = true;
            telemetryRepository.Save();

            EnableAllEvents();
        }

        public async void OptOut()
        {
            telemetryRepository.Data.IsAnonymousDataShared = false;
            telemetryRepository.Save();

            DisableAllEvents();

            await telemetryClient.OptOut(GetPayload(telemetryRepository.Data));
        }

        private void DisableAllEvents()
        {
            telemetryTimer.Elapsed -= OnTelemetryTimerElapsed;
            telemetryTimer.Stop();

            knownUIContexts.SolutionBuildingContextChanged -= this.OnAnalysisRun;
            knownUIContexts.SolutionExistsAndFullyLoadedContextChanged -= this.OnAnalysisRun;
        }

        private void EnableAllEvents()
        {
            telemetryTimer.Elapsed += OnTelemetryTimerElapsed;
            telemetryTimer.Start();

            knownUIContexts.SolutionBuildingContextChanged += this.OnAnalysisRun;
            knownUIContexts.SolutionExistsAndFullyLoadedContextChanged += this.OnAnalysisRun;
        }

        private TelemetryPayload GetPayload(TelemetryData telemetryData)
        {
            var numberOfDaysSinceInstallation = (long)DateTime.Now.DaysPassedSince(telemetryData.InstallationDate);
            return new TelemetryPayload
            {
                SonarLintProduct = "SonarLint Visual Studio",
                SonarLintVersion = TelemetryHelper.SonarLintVersion,
                VisualStudioVersion = TelemetryHelper.VisualStudioVersion,
                NumberOfDaysSinceInstallation = numberOfDaysSinceInstallation,
                NumberOfDaysOfUse = telemetryData.NumberOfDaysOfUse,
                IsUsingConnectedMode = this.solutionBindingTracker.IsActiveSolutionBound
            };
        }

        private void OnAnalysisRun(object sender, UIContextChangedEventArgs e)
        {
            if (!e.Activated)
            {
                return;
            }

            var lastAnalysisDate = telemetryRepository.Data.LastSavedAnalysisDate.Date;
            if (!DateTime.Today.IsSameDay(lastAnalysisDate))
            {
                telemetryRepository.Data.LastSavedAnalysisDate = DateTime.Today;
                telemetryRepository.Data.NumberOfDaysOfUse++;
                telemetryRepository.Save();
            }
        }

        private async void OnTelemetryTimerElapsed(object sender, TelemetryTimerEventArgs e)
        {
            telemetryRepository.Data.LastUploadDate = e.SignalTime;
            telemetryRepository.Save();

            await telemetryClient.SendPayload(GetPayload(telemetryRepository.Data));
        }
    }
}
