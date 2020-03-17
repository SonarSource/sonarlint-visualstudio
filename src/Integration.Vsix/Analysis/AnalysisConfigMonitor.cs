/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Suppression;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    internal interface IAnalysisConfigMonitor
    {
        // Marker interface
    }

    /// <summary>
    /// Monitors configuration changes that can affect analysis results and requests
    /// re-analysis.
    /// </summary>
    [Export(typeof(IAnalysisConfigMonitor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class AnalysisConfigMonitor : IAnalysisConfigMonitor, IDisposable
    {
        private readonly IAnalysisRequester analysisRequester;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IUserSettingsProvider userSettingsProvider;
        private readonly ISuppressedIssuesMonitor suppressedIssuesMonitor;
        private readonly ILogger logger;

        [ImportingConstructor]
        public AnalysisConfigMonitor(IAnalysisRequester analysisRequester,
            IUserSettingsProvider userSettingsProvider, // reports changes to user settings.json
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            ISuppressedIssuesMonitor suppressedIssuesMonitor, // reports changes to connected mode
            ILogger logger)
        {
            this.analysisRequester = analysisRequester;
            this.userSettingsProvider = userSettingsProvider;
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.suppressedIssuesMonitor = suppressedIssuesMonitor;
            this.logger = logger;

            userSettingsProvider.SettingsChanged += OnUserSettingsChanged;
            suppressedIssuesMonitor.SuppressionsUpdated += OnSuppressionsUpdated;
        }

        private void OnUserSettingsChanged(object sender, EventArgs e)
        {
            // NB assumes exception handling is done by the AnalysisRequester
            if (activeSolutionBoundTracker.CurrentConfiguration.Mode == NewConnectedMode.SonarLintMode.Standalone)
            {
                logger.WriteLine(AnalysisStrings.ConfigMonitor_UserSettingsChanged);
                analysisRequester.RequestAnalysis();
            }
            else
            {
                logger.WriteLine(AnalysisStrings.ConfigMonitor_IgnoringUserSettingsChanged);
            }
        }

        private void OnSuppressionsUpdated(object sender, EventArgs e)
        {
            logger.WriteLine(AnalysisStrings.ConfigMonitor_BindingUpdated);
            analysisRequester.RequestAnalysis();
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
                    suppressedIssuesMonitor.SuppressionsUpdated -= OnSuppressionsUpdated;
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
