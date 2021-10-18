/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Linq;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Core.VsVersion;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(ITelemetryManager)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class TelemetryManager : ITelemetryManager, IDisposable
    {
        private readonly IActiveSolutionBoundTracker solutionBindingTracker;
        private readonly ITelemetryClient telemetryClient;
        private readonly ILogger logger;
        private readonly ITelemetryTimer telemetryTimer;
        private readonly ITelemetryDataRepository telemetryRepository;
        private readonly IKnownUIContexts knownUIContexts;
        private readonly ICurrentTimeProvider currentTimeProvider;
        private readonly IVsVersion vsVersion;

        [ImportingConstructor]
        public TelemetryManager(IActiveSolutionBoundTracker solutionBindingTracker, 
            ITelemetryDataRepository telemetryRepository,
            IVsVersionProvider vsVersionProvider,
            ILogger logger)
            : this(solutionBindingTracker, telemetryRepository, vsVersionProvider, logger,
                  new TelemetryClient(), new TelemetryTimer(telemetryRepository, new TimerFactory()),
                  new KnownUIContextsWrapper(), DefaultCurrentTimeProvider.Instance)
        {
        }

        public TelemetryManager(IActiveSolutionBoundTracker solutionBindingTracker, ITelemetryDataRepository telemetryRepository, IVsVersionProvider vsVersionProvider,
            ILogger logger, ITelemetryClient telemetryClient, ITelemetryTimer telemetryTimer, IKnownUIContexts knownUIContexts,
            ICurrentTimeProvider currentTimeProvider)
        {
            this.solutionBindingTracker = solutionBindingTracker;
            this.telemetryRepository = telemetryRepository;
            this.logger = logger;
            this.telemetryClient = telemetryClient;
            this.telemetryTimer = telemetryTimer;
            this.knownUIContexts = knownUIContexts;
            this.currentTimeProvider = currentTimeProvider;

            vsVersion = vsVersionProvider.Version;

            if (this.telemetryRepository.Data.InstallationDate == DateTimeOffset.MinValue)
            {
                this.telemetryRepository.Data.InstallationDate = currentTimeProvider.Now;
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

            await telemetryClient.OptOutAsync(GetPayload(telemetryRepository.Data));
        }

        private void DisableAllEvents()
        {
            telemetryTimer.Elapsed -= OnTelemetryTimerElapsed;
            telemetryTimer.Stop();

            knownUIContexts.SolutionBuildingContextChanged -= this.OnAnalysisRun;
            knownUIContexts.SolutionExistsAndFullyLoadedContextChanged -= this.OnAnalysisRun;
            knownUIContexts.CSharpProjectContextChanged -= OnCSharpProjectContextChanged;
            knownUIContexts.VBProjectContextChanged -= OnVBProjectContextChanged;
        }

        private void EnableAllEvents()
        {
            telemetryTimer.Elapsed += OnTelemetryTimerElapsed;
            telemetryTimer.Start();

            knownUIContexts.SolutionBuildingContextChanged += this.OnAnalysisRun;
            knownUIContexts.SolutionExistsAndFullyLoadedContextChanged += this.OnAnalysisRun;
            knownUIContexts.CSharpProjectContextChanged += OnCSharpProjectContextChanged;
            knownUIContexts.VBProjectContextChanged += OnVBProjectContextChanged;
        }

        private void OnCSharpProjectContextChanged(object sender, UIContextChangedEventArgs e)
        {
            if (e.Activated)
            {
                LanguageAnalyzed(SonarLanguageKeys.CSharp);
            }
        }

        private void OnVBProjectContextChanged(object sender, UIContextChangedEventArgs e)
        {
            if (e.Activated)
            {
                LanguageAnalyzed(SonarLanguageKeys.VBNet);
            }
        }

        private TelemetryPayload GetPayload(TelemetryData telemetryData)
        {
            return TelemetryHelper.CreatePayload(telemetryData,
                currentTimeProvider.Now,
                solutionBindingTracker.CurrentConfiguration,
                vsVersion);
        }

        private void OnAnalysisRun(object sender, UIContextChangedEventArgs e)
        {
            if (e.Activated)
            {
                this.Update();
            }
        }

        public void Update()
        {
            try
            {
                var lastAnalysisDate = telemetryRepository.Data.LastSavedAnalysisDate;
                var now = currentTimeProvider.Now;
                if (!now.IsSameDay(lastAnalysisDate, currentTimeProvider.LocalTimeZone))
                {
                    // Fix up bad days_of_use data. See #1440: https://github.com/SonarSource/sonarlint-visualstudio/issues/1440
                    var maxPossibleDaysOfUse = now.DaysPassedSince(telemetryRepository.Data.InstallationDate) + 1;
                    var daysOfUse = Math.Min(telemetryRepository.Data.NumberOfDaysOfUse + 1, maxPossibleDaysOfUse);

                    telemetryRepository.Data.LastSavedAnalysisDate = now;
                    telemetryRepository.Data.NumberOfDaysOfUse = daysOfUse;
                    telemetryRepository.Save();
                }
            }
            catch (Exception ex) when (!Core.ErrorHandler.IsCriticalException(ex))
            {
                // Suppress non-critical exceptions
                logger.WriteLine(Resources.Strings.Telemetry_ERROR_Recording, ex.Message);
            }
        }

        public void LanguageAnalyzed(string languageKey)
        {
            Debug.Assert(!string.IsNullOrEmpty(languageKey), "Supplied languageKey should not be null/empty");
            Debug.Assert(telemetryRepository.Data != null);

            if (!telemetryRepository.Data.Analyses.Any(x => string.Equals(x.Language, languageKey, StringComparison.OrdinalIgnoreCase)))
            {
                telemetryRepository.Data.Analyses.Add(new Analysis { Language = languageKey });
                telemetryRepository.Save();
            }
        }

        public void ShowHotspotRequested()
        {
            Debug.Assert(telemetryRepository.Data != null);

            ++telemetryRepository.Data.ShowHotspot.NumberOfRequests;
            telemetryRepository.Save();
        }

        public void TaintIssueInvestigatedLocally()
        {
            Debug.Assert(telemetryRepository.Data != null);

            ++telemetryRepository.Data.TaintVulnerabilities.NumberOfIssuesInvestigatedLocally;
            telemetryRepository.Save();
        }

        public void TaintIssueInvestigatedRemotely()
        {
            Debug.Assert(telemetryRepository.Data != null);

            ++telemetryRepository.Data.TaintVulnerabilities.NumberOfIssuesInvestigatedRemotely;
            telemetryRepository.Save();
        }

        private async void OnTelemetryTimerElapsed(object sender, TelemetryTimerEventArgs e)
        {
            try
            {
                telemetryRepository.Data.LastUploadDate = e.SignalTime;

                await telemetryClient.SendPayloadAsync(GetPayload(telemetryRepository.Data));

                // Reset daily data
                telemetryRepository.Data.Analyses = new System.Collections.Generic.List<Analysis>();
                telemetryRepository.Data.ShowHotspot = new ShowHotspot();
                telemetryRepository.Data.TaintVulnerabilities = new TaintVulnerabilities();
                telemetryRepository.Data.ServerNotifications = new ServerNotifications();
                telemetryRepository.Data.CFamilyProjectTypes = new CFamilyProjectTypes();
                telemetryRepository.Data.RulesUsage = new RulesUsage();
                telemetryRepository.Save();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Suppress non-critical exceptions
                logger.WriteLine(Resources.Strings.Telemetry_ERROR_SendingTelemetry, ex.Message);
            }
        }
    }
}
