using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.Integration.Vsix.SupportedLanguages;

internal enum ConnectionBannerState
{
    NoConnection,
    NotBound,
    Bound,
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
    private readonly IConnectedModeUIManager connectedModeUIManager;
    private readonly ISLCoreHandler slCoreHandler;

    public ObservableCollection<PluginStatusDto> AllPlugins { get; }

    public ObservableCollection<PluginStatusDto> DisplayedPlugins { get; }

    public string PremiumLanguagesTooltip =>
        string.Join(", ", AllPlugins
            .Where(p => p.state == PluginStateDto.PREMIUM)
            .Select(p => p.pluginName)
            .Distinct());

    public bool IsBannerVisible => GetConnectionBannerState() != ConnectionBannerState.Bound;
    public bool IsErrorBanner => GetConnectionBannerState() == ConnectionBannerState.PluginFailed;
    public string SetUpConnectionText => GetConnectionBannerState() == ConnectionBannerState.NotBound ? "Bind Project" : "Set up Connection";
    public string FailedPluginsText =>
        string.Join(", ", AllPlugins
            .Where(p => p.state == PluginStateDto.FAILED)
            .Select(p => p.pluginName));

    public SupportedLanguagesDialogViewModel(
        IPluginStatusesStore pluginStatusesStore,
        IThreadHandling threadHandling,
        IServerConnectionsRepository serverConnectionsRepository,
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IConnectedModeUIManager connectedModeUIManager,
        ISLCoreHandler slCoreHandler)
    {
        this.pluginStatusesStore = pluginStatusesStore;
        this.threadHandling = threadHandling;
        this.serverConnectionsRepository = serverConnectionsRepository;
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.connectedModeUIManager = connectedModeUIManager;
        this.slCoreHandler = slCoreHandler;

        AllPlugins = new ObservableCollection<PluginStatusDto>(pluginStatusesStore.GetAll());
        DisplayedPlugins = new ObservableCollection<PluginStatusDto>(
            AllPlugins.Where(p => DisplayedStates.Contains(p.state)));

        pluginStatusesStore.PluginStatusesChanged += OnPluginStatusesChanged;
        serverConnectionsRepository.ConnectionChanged += OnConnectionStateChanged;
        activeSolutionBoundTracker.SolutionBindingChanged += OnConnectionStateChanged;
    }

    public void SetUpConnection() => connectedModeUIManager.ShowManageBindingDialogAsync().Forget();

    public void RestartBackend() => slCoreHandler.ForceRestartSloop();

    public void Dispose()
    {
        pluginStatusesStore.PluginStatusesChanged -= OnPluginStatusesChanged;
        serverConnectionsRepository.ConnectionChanged -= OnConnectionStateChanged;
        activeSolutionBoundTracker.SolutionBindingChanged -= OnConnectionStateChanged;
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
            return ConnectionBannerState.Bound;
        }
        if (serverConnectionsRepository.TryGetAll(out var connections) && connections.Count > 0)
        {
            return ConnectionBannerState.NotBound;
        }
        return ConnectionBannerState.NoConnection;
    }
}
