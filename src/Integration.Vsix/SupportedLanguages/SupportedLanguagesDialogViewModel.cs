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

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
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
    private readonly IThreadHandling threadHandling;
    private readonly IServerConnectionsRepository serverConnectionsRepository;
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly IActiveSolutionTracker activeSolutionTracker;
    private readonly IConnectedModeUIManager connectedModeUIManager;
    private readonly ISLCoreHandler slCoreHandler;

    public ObservableCollection<PluginStatusDto> AllPlugins { get; }

    public ObservableCollection<PluginStatusDto> DisplayedPlugins { get; }

    public string PremiumLanguagesTooltip
    {
        get
        {
            var names = string.Join(", ", AllPlugins
                .Where(p => p.state == PluginStateDto.PREMIUM)
                .Select(p => p.pluginName)
                .Distinct());
            return names.Length == 0 ? string.Empty : string.Format(Strings.PluginStatuses_PremiumLanguagesTooltip, names);
        }
    }

    public bool IsBannerVisible => GetConnectionBannerState() != ConnectionBannerState.Hidden;
    public bool IsErrorBanner => GetConnectionBannerState() == ConnectionBannerState.PluginFailed;
    public string SetUpConnectionText => GetConnectionBannerState() == ConnectionBannerState.NotBound ? Strings.PluginStatuses_BannerBindProjectButton : Strings.PluginStatuses_BannerSetUpConnectionButton;
    public string FailedPluginsText =>
        string.Join(", ", AllPlugins
            .Where(p => p.state == PluginStateDto.FAILED)
            .Select(p => p.pluginName));

    public SupportedLanguagesDialogViewModel(
        IPluginStatusesStore pluginStatusesStore,
        IThreadHandling threadHandling,
        IServerConnectionsRepository serverConnectionsRepository,
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IActiveSolutionTracker activeSolutionTracker,
        IConnectedModeUIManager connectedModeUIManager,
        ISLCoreHandler slCoreHandler)
    {
        this.pluginStatusesStore = pluginStatusesStore;
        this.threadHandling = threadHandling;
        this.serverConnectionsRepository = serverConnectionsRepository;
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.activeSolutionTracker = activeSolutionTracker;
        this.connectedModeUIManager = connectedModeUIManager;
        this.slCoreHandler = slCoreHandler;

        AllPlugins = new ObservableCollection<PluginStatusDto>(pluginStatusesStore.GetAll());
        DisplayedPlugins = new ObservableCollection<PluginStatusDto>(
            AllPlugins.Where(p => DisplayedStates.Contains(p.state)));

        pluginStatusesStore.PluginStatusesChanged += OnPluginStatusesChanged;
        serverConnectionsRepository.ConnectionChanged += OnConnectionStateChanged;
        activeSolutionBoundTracker.SolutionBindingChanged += OnConnectionStateChanged;
        activeSolutionTracker.ActiveSolutionChanged += OnConnectionStateChanged;
    }

    public void SetUpConnection() => connectedModeUIManager.ShowManageBindingDialogAsync().Forget();

    public void RestartBackend() => slCoreHandler.ForceRestartSloop();

    public void Dispose()
    {
        pluginStatusesStore.PluginStatusesChanged -= OnPluginStatusesChanged;
        serverConnectionsRepository.ConnectionChanged -= OnConnectionStateChanged;
        activeSolutionBoundTracker.SolutionBindingChanged -= OnConnectionStateChanged;
        activeSolutionTracker.ActiveSolutionChanged -= OnConnectionStateChanged;
    }

    private void OnPluginStatusesChanged(object sender, EventArgs e)
    {
        threadHandling.RunOnUIThread(() =>
        {
            AllPlugins.Clear();
            foreach (var plugin in pluginStatusesStore.GetAll())
            {
                AllPlugins.Add(plugin);
            }

            DisplayedPlugins.Clear();
            foreach (var plugin in AllPlugins.Where(p => DisplayedStates.Contains(p.state)))
            {
                DisplayedPlugins.Add(plugin);
            }

            RaisePropertyChanged(nameof(PremiumLanguagesTooltip));
            RaisePropertyChanged(nameof(FailedPluginsText));
            RaisePropertyChanged(nameof(IsBannerVisible));
            RaisePropertyChanged(nameof(IsErrorBanner));
            RaisePropertyChanged(nameof(SetUpConnectionText));
        });
    }

    private void OnConnectionStateChanged(object sender, EventArgs e)
    {
        threadHandling.RunOnUIThread(() =>
        {
            RaisePropertyChanged(nameof(IsBannerVisible));
            RaisePropertyChanged(nameof(IsErrorBanner));
            RaisePropertyChanged(nameof(SetUpConnectionText));
        });
    }

    private ConnectionBannerState GetConnectionBannerState()
    {
        if (AllPlugins.Any(p => p.state == PluginStateDto.FAILED))
        {
            return ConnectionBannerState.PluginFailed;
        }
        if (activeSolutionBoundTracker.CurrentConfiguration.Mode.IsInAConnectedMode())
        {
            return ConnectionBannerState.Hidden;
        }
        if (activeSolutionTracker.CurrentSolutionName == null)
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
