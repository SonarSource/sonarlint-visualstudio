/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    /// <summary>
    /// Monitors configuration changes that can affect analysis results and requests
    /// re-analysis.
    /// </summary>
    [Export(typeof(IAnalysisConfigMonitor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class AnalysisConfigMonitor : IAnalysisConfigMonitor, IDisposable
    {
        private readonly IAnalysisRequester analysisRequester;
        private readonly IUserSettingsProvider userSettingsProvider;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly INotifyQualityProfilesChanged notifyQualityProfilesUpdated;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;

        public event EventHandler ConfigChanged;

        [ImportingConstructor]
        public AnalysisConfigMonitor(IAnalysisRequester analysisRequester,
            IUserSettingsProvider userSettingsProvider, // reports changes to user settings.json
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            INotifyQualityProfilesChanged notifyQualityProfilesUpdated,
            ILogger logger)
            : this(analysisRequester,
                  userSettingsProvider,
                  activeSolutionBoundTracker,
                  notifyQualityProfilesUpdated,
                  logger,
                  ThreadHandling.Instance)
        { }

        internal AnalysisConfigMonitor(IAnalysisRequester analysisRequester,
            IUserSettingsProvider userSettingsProvider,
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            INotifyQualityProfilesChanged notifyQualityProfilesUpdated,
            ILogger logger,
            IThreadHandling threadHandling)
        {
            this.analysisRequester = analysisRequester;
            this.userSettingsProvider = userSettingsProvider;
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.notifyQualityProfilesUpdated = notifyQualityProfilesUpdated;
            this.logger = logger;
            this.threadHandling = threadHandling;

            userSettingsProvider.SettingsChanged += OnUserSettingsChanged;
            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;
            notifyQualityProfilesUpdated.QualityProfilesChanged += OnQualityProfilesUpdated;
        }

        #region Incoming notifications

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            logger.WriteLine(AnalysisStrings.ConfigMonitor_BindingChanged);
            OnSettingsChangedAsync().Forget();
        }

        private void OnUserSettingsChanged(object sender, EventArgs e)
        {
            // There is a corner-case where we want to raise the event even in Connected Mode - see https://github.com/SonarSource/sonarlint-visualstudio/issues/3701
            logger.WriteLine(AnalysisStrings.ConfigMonitor_UserSettingsChanged);
            OnSettingsChangedAsync().Forget();
        }

        private void OnQualityProfilesUpdated(object sender, EventArgs e)
        {
            logger.WriteLine(AnalysisStrings.ConfigMonitor_QualityProfilesChanged);
            OnSettingsChangedAsync().Forget();
        }

        #endregion Incoming notifications

        private async Task OnSettingsChangedAsync()
        {
            await threadHandling.SwitchToBackgroundThread();
            RaiseConfigChangedEvent();

            // NB assumes exception handling is done by the AnalysisRequester
            analysisRequester.RequestAnalysis();
        }

        private void RaiseConfigChangedEvent()
        {
            ConfigChanged?.Invoke(this, EventArgs.Empty);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    userSettingsProvider.SettingsChanged -= OnUserSettingsChanged;
                    activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
                    notifyQualityProfilesUpdated.QualityProfilesChanged -= OnQualityProfilesUpdated;
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
