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
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.NewCode;

namespace SonarLint.VisualStudio.IssueVisualization.NewCode;

[Export(typeof(IFocusOnNewCodeService))]
[Export(typeof(IFocusOnNewCodeServiceUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class FocusOnNewCodeService : IFocusOnNewCodeServiceUpdater
{
    private readonly object lockObject = new();
    private readonly ISonarLintSettings sonarLintSettings;
    private readonly ISLCoreServiceProvider slCoreServiceProvider;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private readonly (string FocusOnNewCodeNotAvailable, bool) unavailableNewCode = (Resources.FocusOnNewCodeNotAvailableDescription, true);

    [ImportingConstructor]
    public FocusOnNewCodeService(
        ISonarLintSettings sonarLintSettings,
        IInitializationProcessorFactory initializationProcessorFactory,
        ISLCoreServiceProvider slCoreServiceProvider,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IThreadHandling threadHandling,
        ILogger logger)
    {
        this.sonarLintSettings = sonarLintSettings;
        this.slCoreServiceProvider = slCoreServiceProvider;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.threadHandling = threadHandling;
        this.logger = logger.ForContext(Resources.FocusOnNewCodeServiceLogContext);
        InitializationProcessor = initializationProcessorFactory.CreateAndStart<FocusOnNewCodeService>([], async _ =>
        {
            // sonarLintSettings needs UI thread to initialize settings storage, so the first property access may not be free-threaded
            activeConfigScopeTracker.CurrentConfigurationScopeChanged += ActiveConfigScopeTracker_CurrentConfigurationScopeChanged;
            await RefreshCurrentStatusAsync();
        });
    }

    public IInitializationProcessor InitializationProcessor { get; }
    public FocusOnNewCodeStatus Current { get; private set; } = new(default, default, string.Empty);
    public event EventHandler<NewCodeStatusChangedEventArgs> Changed;

    public void SetPreference(bool isEnabled)
    {
        sonarLintSettings.IsFocusOnNewCodeEnabled = isEnabled;
        FocusOnNewCodeStatus updated;
        lock (lockObject)
        {
            updated = Current with { IsEnabled = isEnabled };
            Current = updated;
        }
        NotifySlCorePreferenceChanged();
        NotifyStatusChanged(updated);
    }

    private async Task<FocusOnNewCodeStatus> RefreshCurrentStatusAsync()
    {
        var newStatus = await GetNewStatusAsync();
        lock (lockObject)
        {
            if (newStatus == Current)
            {
                return null;
            }

            Current = newStatus;
            return newStatus;
        }
    }

    private void ActiveConfigScopeTracker_CurrentConfigurationScopeChanged(object sender, ConfigurationScopeChangedEventArgs e) =>
        RefreshCurrentStatusAndNotifyAsync().Forget();

    private async Task RefreshCurrentStatusAndNotifyAsync()
    {
        if (await RefreshCurrentStatusAsync() is {} updated)
        {
            NotifyStatusChanged(updated);
        }
    }

    private async Task<FocusOnNewCodeStatus> GetNewStatusAsync()
    {
        var isEnabled = sonarLintSettings.IsFocusOnNewCodeEnabled;
        var (description, isSupported) = await SafeGetNewCodeDefinitionAsync();
        var newStatus = new FocusOnNewCodeStatus(isEnabled, isSupported, description);
        return newStatus;
    }

    private async Task<(string description, bool isSupported)> SafeGetNewCodeDefinitionAsync()
    {
        try
        {
            if (activeConfigScopeTracker.Current is not { Id: { } configScopeId })
            {
                return GetNewCodeUnavailable(SLCoreStrings.ConfigScopeNotInitialized);
            }
            if (!slCoreServiceProvider.TryGetTransientService(out INewCodeSLCoreService newCodeService))
            {
                return GetNewCodeUnavailable(SLCoreStrings.ServiceProviderNotInitialized);
            }

            var (description, isSupported) = await newCodeService.GetNewCodeDefinitionAsync(new(configScopeId));
            return (description, isSupported);
        }
        catch (Exception e)
        {
            return GetNewCodeUnavailable(e.ToString());
        }
    }

    private (string description, bool isSupported) GetNewCodeUnavailable(string reason)
    {
        logger.WriteLine(Resources.FocusOnNewCodeDefinitionUnavailableLogTemplate, reason);
        return unavailableNewCode;
    }

    private void NotifyStatusChanged(FocusOnNewCodeStatus updated) => Changed?.Invoke(this, new(updated));

    private void NotifySlCorePreferenceChanged() =>
        threadHandling.RunOnBackgroundThread(() =>
        {
            if (slCoreServiceProvider.TryGetTransientService(out INewCodeSLCoreService newCodeService))
            {
                newCodeService.DidToggleFocus();
            }
        }).Forget();
}
