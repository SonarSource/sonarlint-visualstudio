/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.Integration.Vsix.SupportedLanguages;

internal enum ConnectionBannerState
{
    NoConnection,
    NotBound,
    Hidden,
    PluginFailed
}

internal sealed class SupportedLanguagesDialogViewModel : ViewModelBase, IDisposable
{
    private static readonly HashSet<PluginStateDto> DisplayedStates =
    [
        PluginStateDto.ACTIVE,
        PluginStateDto.SYNCED,
        PluginStateDto.DOWNLOADING,
        PluginStateDto.FAILED
    ];

    private readonly IPluginStatusesStore pluginStatusesStore;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly ISLCoreHandler slCoreHandler;
    private readonly IServerConnectionsRepository serverConnectionsRepository;
    private readonly IConnectedModeUIManager connectedModeUiManager;
    private readonly IThreadHandling threadHandling;
    private readonly ITelemetryManager telemetryManager;

    public ObservableCollection<PluginStatusDisplay> AllPlugins { get; } = new ();

    public ObservableCollection<PluginStatusDisplay> DisplayedPlugins { get; } = new ();

    public string PremiumPluginsTooltip
    {
        get
        {
            var names = string.Join(", ", AllPlugins
                .Where(p => p.State == PluginStateDto.PREMIUM)
                .Select(p => p.PluginName)
                .Distinct());
            return names.Length == 0 ? string.Empty : string.Format(Strings.PluginStatuses_PremiumPluginsTooltip, names);
        }
    }
    public string FailedPluginsTooltip =>
        string.Join(", ", AllPlugins
            .Where(p => p.State == PluginStateDto.FAILED)
            .Select(p => p.PluginName));

    public ConnectionBannerState BannerState { get; private set; }
    public bool IsBannerError => BannerState == ConnectionBannerState.PluginFailed;
    public bool IsBannerPromotion => BannerState == ConnectionBannerState.NotBound || BannerState == ConnectionBannerState.NoConnection;
    public string PromotionBannerButtonText => BannerState switch
        {
            ConnectionBannerState.NotBound => Strings.PluginStatuses_BannerBindProjectButton,
            ConnectionBannerState.NoConnection => Strings.PluginStatuses_BannerSetUpConnectionButton,
            _ => string.Empty
        };

    public SupportedLanguagesDialogViewModel(
        IPluginStatusesStore pluginStatusesStore,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        ISLCoreHandler slCoreHandler,
        IServerConnectionsRepository serverConnectionsRepository,
        IConnectedModeUIManager connectedModeUiManager,
        IThreadHandling threadHandling,
        ITelemetryManager telemetryManager)
    {
        this.pluginStatusesStore = pluginStatusesStore;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.slCoreHandler = slCoreHandler;
        this.serverConnectionsRepository = serverConnectionsRepository;
        this.connectedModeUiManager = connectedModeUiManager;
        this.threadHandling = threadHandling;
        this.telemetryManager = telemetryManager;

        pluginStatusesStore.PluginStatusesChanged += PluginStatusesStore_PluginStatusesChanged;
        serverConnectionsRepository.ConnectionChanged += ServerConnectionsRepository_ConnectionChanged;
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += ActiveConfigScopeTracker_CurrentConfigurationScopeChanged;

        UpdateFullStateAsync().Forget();
    }

    public void SetUpConnection()
    {
        telemetryManager.SupportedLanguagesPanelCtaClicked();
        connectedModeUiManager.ShowManageBindingDialogAsync().Forget();
    }

    public void RestartBackend()
    {
        slCoreHandler.ForceRestartSloop();
        pluginStatusesStore.Clear();
    }

    public void Dispose()
    {
        pluginStatusesStore.PluginStatusesChanged -= PluginStatusesStore_PluginStatusesChanged;
        serverConnectionsRepository.ConnectionChanged -= ServerConnectionsRepository_ConnectionChanged;
        activeConfigScopeTracker.CurrentConfigurationScopeChanged -= ActiveConfigScopeTracker_CurrentConfigurationScopeChanged;
    }

    private void PluginStatusesStore_PluginStatusesChanged(object sender, EventArgs e) => UpdateFullStateAsync().Forget();

    private void ServerConnectionsRepository_ConnectionChanged(object sender, EventArgs e) => UpdateFullStateAsync().Forget();

    private void ActiveConfigScopeTracker_CurrentConfigurationScopeChanged(object sender, EventArgs e) => UpdateFullStateAsync().Forget();

    private async Task UpdateFullStateAsync()
    {
        ConfigurationScope configScope = null;

        await threadHandling.RunOnBackgroundThread(() =>
        {
            configScope = activeConfigScopeTracker.Current;
        });

        await threadHandling.RunOnUIThreadAsync(() =>
        {
            UpdatePlugins();
            UpdateBannerState(configScope);
        });
    }

    private void UpdateBannerState(ConfigurationScope configScope)
    {
        BannerState = GetConnectionBannerState(configScope);

        RaisePropertyChanged(nameof(BannerState));
        RaisePropertyChanged(nameof(IsBannerError));
        RaisePropertyChanged(nameof(IsBannerPromotion));
        RaisePropertyChanged(nameof(PromotionBannerButtonText));
    }

    private void UpdatePlugins()
    {
        AllPlugins.Clear();
        foreach (var plugin in pluginStatusesStore.GetAll())
        {
            AllPlugins.Add(plugin);
        }

        DisplayedPlugins.Clear();
        foreach (var plugin in AllPlugins.Where(p => DisplayedStates.Contains(p.State)))
        {
            DisplayedPlugins.Add(plugin);
        }

        RaisePropertyChanged(nameof(PremiumPluginsTooltip));
        RaisePropertyChanged(nameof(FailedPluginsTooltip));
    }

    private ConnectionBannerState GetConnectionBannerState(ConfigurationScope configScope)
    {
        if (AllPlugins.Any(p => p.State == PluginStateDto.FAILED))
        {
            return ConnectionBannerState.PluginFailed;
        }
        // If no solution is open OR a project is bound
        if (configScope is null or { SonarProjectId: not null })
        {
            return ConnectionBannerState.Hidden;
        }
        if (serverConnectionsRepository.TryGetAll(out var connections) && connections.Count > 0)
        {
            return ConnectionBannerState.NotBound;
        }
        return ConnectionBannerState.NoConnection;
    }
}
