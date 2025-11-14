/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;

namespace SonarLint.VisualStudio.SLCore.Analysis;

public interface ISlCoreUserAnalysisPropertiesSynchronizer : IRequireInitialization, IDisposable;

[Export(typeof(ISlCoreUserAnalysisPropertiesSynchronizer))]
[PartCreationPolicy(CreationPolicy.Shared)]
public sealed class SlCoreUserAnalysisPropertiesSynchronizer : ISlCoreUserAnalysisPropertiesSynchronizer
{
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IUserSettingsProvider userSettingsProvider;
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IThreadHandling threadHandling;
    private bool isDisposed;
    public IInitializationProcessor InitializationProcessor { get; }

    [ImportingConstructor]
    public SlCoreUserAnalysisPropertiesSynchronizer(
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IUserSettingsProvider userSettingsProvider,
        IInitializationProcessorFactory initializationProcessorFactory,
        ISLCoreServiceProvider serviceProvider,
        IThreadHandling threadHandling)
    {
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.userSettingsProvider = userSettingsProvider;
        this.serviceProvider = serviceProvider;
        this.threadHandling = threadHandling;
        InitializationProcessor = initializationProcessorFactory.CreateAndStart<SlCoreUserAnalysisPropertiesSynchronizer>(
            [userSettingsProvider],
            () =>
            {
                if (isDisposed)
                {
                    return;
                }
                activeConfigScopeTracker.CurrentConfigurationScopeChanged += ActiveConfigScopeTracker_CurrentConfigurationScopeChanged;
                userSettingsProvider.SettingsChanged += UserSettingsProvider_SettingsChanged;
                HandleSettingsChange();
            });
    }

    private void UserSettingsProvider_SettingsChanged(object sender, EventArgs e) =>
        threadHandling.RunOnBackgroundThread(HandleSettingsChange).Forget();

    private void ActiveConfigScopeTracker_CurrentConfigurationScopeChanged(object sender, ConfigurationScopeChangedEventArgs e)
    {
        if (e.DefinitionChanged)
        {
            threadHandling.RunOnBackgroundThread(HandleSettingsChange).Forget();
        }
    }

    private void HandleSettingsChange()
    {
        if (activeConfigScopeTracker.Current is { Id: { } currentConfigScopeId } && serviceProvider.TryGetTransientService(out IUserAnalysisPropertiesService? userAnalysisPropertiesService))
        {
            userAnalysisPropertiesService.DidSetUserAnalysisProperties(new(currentConfigScopeId, userSettingsProvider.UserSettings.AnalysisSettings.AnalysisProperties));
        }
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        if (InitializationProcessor.IsFinalized)
        {
            activeConfigScopeTracker.CurrentConfigurationScopeChanged -= ActiveConfigScopeTracker_CurrentConfigurationScopeChanged;
            userSettingsProvider.SettingsChanged -= UserSettingsProvider_SettingsChanged;
        }
        isDisposed = true;
    }
}
