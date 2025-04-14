﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;
using SonarLint.VisualStudio.SLCore.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis;

/// <summary>
/// Monitors configuration changes that can affect analysis results and requests
/// re-analysis.
/// </summary>
[Export(typeof(IAnalysisConfigMonitor))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class AnalysisConfigMonitor : IAnalysisConfigMonitor, IDisposable
{
    private readonly IAnalysisRequester analysisRequester;
    private readonly IUserSettingsProvider userSettingsUpdater;
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly ILogger logger;
    private readonly IThreadHandling threadHandling;
    private readonly ISLCoreRuleSettingsUpdater slCoreRuleSettingsUpdater;
    private readonly IStandaloneRoslynSettingsUpdater roslynSettingsUpdater;

    [ImportingConstructor]
    public AnalysisConfigMonitor(
        IAnalysisRequester analysisRequester,
        IUserSettingsProvider userSettingsUpdater,
        ISLCoreRuleSettingsUpdater slCoreRuleSettingsUpdater,
        IStandaloneRoslynSettingsUpdater roslynSettingsUpdater,
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        ILogger logger,
        IThreadHandling threadHandling)
    {
        this.analysisRequester = analysisRequester;
        this.userSettingsUpdater = userSettingsUpdater;
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.logger = logger;
        this.threadHandling = threadHandling;
        this.slCoreRuleSettingsUpdater = slCoreRuleSettingsUpdater;
        this.roslynSettingsUpdater = roslynSettingsUpdater;

        // todo https://sonarsource.atlassian.net/browse/SLVS-2025 fix this MEF importing constructor is not free-threaded
        roslynSettingsUpdater.Update(userSettingsUpdater.UserSettings);
        userSettingsUpdater.SettingsChanged += OnUserSettingsChanged;
        activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;
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
        roslynSettingsUpdater.Update(userSettingsUpdater.UserSettings);
        slCoreRuleSettingsUpdater.UpdateStandaloneRulesConfiguration();
        OnSettingsChangedAsync().Forget();
    }

    #endregion Incoming notifications

    private async Task OnSettingsChangedAsync()
    {
        await threadHandling.SwitchToBackgroundThread();

        // NB assumes exception handling is done by the AnalysisRequester
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
                userSettingsUpdater.SettingsChanged -= OnUserSettingsChanged;
                activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
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
