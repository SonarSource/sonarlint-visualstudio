using System.Collections.ObjectModel;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.Integration.Vsix.SupportedLanguages;

internal class SupportedLanguageDialogViewModel : ViewModelBase
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

    public ObservableCollection<PluginStatusDto> AllPlugins { get; }

    public ObservableCollection<PluginStatusDto> DisplayedPlugins { get; }

    public string PremiumLanguagesTooltip =>
        string.Join(", ", AllPlugins
            .Where(p => p.state == PluginStateDto.PREMIUM)
            .Select(p => p.pluginName)
            .Distinct());

    public SupportedLanguageDialogViewModel(IPluginStatusesStore pluginStatusesStore, IThreadHandling threadHandling)
    {
        this.pluginStatusesStore = pluginStatusesStore;
        this.threadHandling = threadHandling;

        AllPlugins = new ObservableCollection<PluginStatusDto>(pluginStatusesStore.GetAll());
        DisplayedPlugins = new ObservableCollection<PluginStatusDto>(
            AllPlugins.Where(p => DisplayedStates.Contains(p.state)));

        pluginStatusesStore.PluginStatusesChanged += OnPluginStatusesChanged;
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
        });
    }
}
