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
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;
using SonarLint.VisualStudio.SLCore.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis;

/// <summary>
/// Monitors configuration changes that can affect analysis results and requests
/// re-analysis.
/// </summary>
[Export(typeof(IAnalysisConfigMonitor))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class AnalysisConfigMonitor : IAnalysisConfigMonitor
{
    private readonly IUserSettingsProvider userSettingsProvider;
    private readonly ILogger logger;
    private readonly IThreadHandling threadHandling;
    private readonly ISLCoreRuleSettingsUpdater slCoreRuleSettingsUpdater;
    private readonly IStandaloneRoslynSettingsUpdater roslynSettingsUpdater;

    [ImportingConstructor]
    public AnalysisConfigMonitor(
        IUserSettingsProvider userSettingsProvider,
        ISLCoreRuleSettingsUpdater slCoreRuleSettingsUpdater,
        IStandaloneRoslynSettingsUpdater roslynSettingsUpdater,
        ILogger logger,
        IThreadHandling threadHandling,
        IInitializationProcessorFactory initializationProcessorFactory)
    {
        this.userSettingsProvider = userSettingsProvider;
        this.logger = logger;
        this.threadHandling = threadHandling;
        this.slCoreRuleSettingsUpdater = slCoreRuleSettingsUpdater;
        this.roslynSettingsUpdater = roslynSettingsUpdater;

        InitializationProcessor = initializationProcessorFactory.CreateAndStart<AnalysisConfigMonitor>(
            [userSettingsProvider],
            () =>
            {
                if (disposedValue)
                {
                    return;
                }
                userSettingsProvider.SettingsChanged += OnUserSettingsChanged;
                roslynSettingsUpdater.Update(userSettingsProvider.UserSettings);
            });
    }

    public IInitializationProcessor InitializationProcessor { get; }

    private void OnUserSettingsChanged(object sender, EventArgs e)
    {
        logger.WriteLine(AnalysisStrings.ConfigMonitor_UserSettingsChanged);
        threadHandling.RunOnBackgroundThread(() =>
            {
                roslynSettingsUpdater.Update(userSettingsProvider.UserSettings);
                slCoreRuleSettingsUpdater.UpdateStandaloneRulesConfiguration();
            }
        ).Forget();
    }

    private bool disposedValue = false; // To detect redundant calls

    public void Dispose()
    {
        if (!disposedValue)
        {
            if (InitializationProcessor.IsFinalized)
            {
                userSettingsProvider.SettingsChanged -= OnUserSettingsChanged;
            }
            disposedValue = true;
        }
    }
}
